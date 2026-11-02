using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{
    // Репозиторій таблиці Books
    public class BookRepository : IBookRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public BookRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Use case 1: сторінка каталогу. Сортування завжди має другий ключ (назву), щоб порядок між сторінками був стабільним
        public async Task<IReadOnlyList<Book>> GetCatalogPageAsync(
            string? genre,
            int? year,
            CatalogSortBy sortBy,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var books = ApplyCatalogFilters(context.Books.AsNoTracking(), genre, year);

            IOrderedQueryable<Book> sorted = sortBy switch
            {
                CatalogSortBy.Author => books.OrderBy(book => book.Author).ThenBy(book => book.Title),
                CatalogSortBy.YearNewest => books.OrderByDescending(book => book.Year).ThenBy(book => book.Title),
                CatalogSortBy.YearOldest => books.OrderBy(book => book.Year).ThenBy(book => book.Title),
                _ => books.OrderBy(book => book.Title).ThenBy(book => book.Author)
            };

            return await sorted
                .ThenBy(book => book.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        // Use case 1: кількість книг під фільтрами
        public async Task<int> CountCatalogAsync(string? genre, int? year, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await ApplyCatalogFilters(context.Books, genre, year).CountAsync(cancellationToken);
        }

        // Use case 1: різні жанри для випадайки
        public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .Where(book => book.Genre != string.Empty)
                .Select(book => book.Genre)
                .Distinct()
                .OrderBy(genre => genre)
                .ToListAsync(cancellationToken);
        }

        // Для випадайок: усі книги, відсортовані за назвою та автором
        public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AsNoTracking()
                .OrderBy(book => book.Title)
                .ThenBy(book => book.Author)
                .ToListAsync(cancellationToken);
        }

        // Use case 2: пошук за назвою (PostgreSQL ILIKE — без урахування регістру)
        public async Task<IReadOnlyList<Book>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            var pattern = $"%{EscapeLikePattern(title)}%";

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AsNoTracking()
                .Where(book => EF.Functions.ILike(book.Title, pattern))
                .OrderBy(book => book.Title)
                .ToListAsync(cancellationToken);
        }

        // Use case 3: пошук за автором (PostgreSQL ILIKE — без урахування регістру)
        public async Task<IReadOnlyList<Book>> SearchByAuthorAsync(string author, CancellationToken cancellationToken = default)
        {
            var pattern = $"%{EscapeLikePattern(author)}%";

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AsNoTracking()
                .Where(book => EF.Functions.ILike(book.Author, pattern))
                .OrderBy(book => book.Author)
                .ThenBy(book => book.Title)
                .ToListAsync(cancellationToken);
        }

        // Use case 8: книга за ідентифікатором
        public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(book => book.Id == id, cancellationToken);
        }

        // Жанр порівнюється повністю, але без урахування регістру (ILIKE без % — точний збіг)
        private static IQueryable<Book> ApplyCatalogFilters(IQueryable<Book> books, string? genre, int? year)
        {
            if (genre is not null)
            {
                var genrePattern = EscapeLikePattern(genre);
                books = books.Where(book => EF.Functions.ILike(book.Genre, genrePattern));
            }

            if (year is not null)
            {
                books = books.Where(book => book.Year == year);
            }

            return books;
        }

        // Екранування спецсимволів LIKE, щоб '%' та '_' у назві чи імені автора шукались як звичайні символи
        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("%", @"\%", StringComparison.Ordinal)
                .Replace("_", @"\_", StringComparison.Ordinal);
        }
    }
}
