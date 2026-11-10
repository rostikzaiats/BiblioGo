using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
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

        // Use case «Популярні книги»: групуємо видачі за книгою. Статус Requested не рахуємо: книгу ще не видали,
        // а IssueDate такої заявки містить лише час її подання
        public async Task<IReadOnlyList<PopularBookDto>> GetPopularAsync(DateTime? issuedFrom, int top, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var loans = context.Loans
                .AsNoTracking()
                .Where(loan => loan.Status != LoanStatus.Requested);

            if (issuedFrom is not null)
            {
                loans = loans.Where(loan => loan.IssueDate >= issuedFrom);
            }

            return await loans
                .GroupBy(loan => new
                {
                    loan.BookCopy.Book.Id,
                    loan.BookCopy.Book.Title,
                    loan.BookCopy.Book.Author,
                    loan.BookCopy.Book.Genre,
                    loan.BookCopy.Book.Year
                })
                .Select(group => new PopularBookDto
                {
                    BookId = group.Key.Id,
                    Title = group.Key.Title,
                    Author = group.Key.Author,
                    Genre = group.Key.Genre,
                    Year = group.Key.Year,
                    IssueCount = group.Count()
                })
                .OrderByDescending(book => book.IssueCount)
                .ThenBy(book => book.Title)
                .ThenBy(book => book.BookId)
                .Take(top)
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

        // Фонд, use case 1: чи є книга з такою назвою та автором (ILIKE без % — точний збіг без урахування регістру)
        public async Task<bool> ExistsAsync(string title, string author, CancellationToken cancellationToken = default)
        {
            var titlePattern = EscapeLikePattern(title);
            var authorPattern = EscapeLikePattern(author);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AnyAsync(
                    book => EF.Functions.ILike(book.Title, titlePattern) && EF.Functions.ILike(book.Author, authorPattern),
                    cancellationToken);
        }

        // Фонд, use case 1: додати книгу до каталогу
        public async Task<int> AddAsync(Book book, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            context.Books.Add(book);
            await context.SaveChangesAsync(cancellationToken);

            return book.Id;
        }

        // Фонд, use case 2: чи є інша книга (з іншим Id) з такою назвою та автором
        public async Task<bool> ExistsAnotherAsync(string title, string author, int excludedBookId, CancellationToken cancellationToken = default)
        {
            var titlePattern = EscapeLikePattern(title);
            var authorPattern = EscapeLikePattern(author);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Books
                .AnyAsync(
                    book => book.Id != excludedBookId
                        && EF.Functions.ILike(book.Title, titlePattern)
                        && EF.Functions.ILike(book.Author, authorPattern),
                    cancellationToken);
        }

        // Фонд, use case 2: оновити дані книги одним UPDATE; false, якщо книги з таким Id немає
        public async Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var updatedRows = await context.Books
                .Where(existing => existing.Id == book.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(existing => existing.Title, book.Title)
                        .SetProperty(existing => existing.Author, book.Author)
                        .SetProperty(existing => existing.Genre, book.Genre)
                        .SetProperty(existing => existing.Year, book.Year)
                        .SetProperty(existing => existing.Description, book.Description),
                    cancellationToken);

            return updatedRows > 0;
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
