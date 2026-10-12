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

        public Task<IEnumerable<Book>> GetCatalogAsync(string? genre, int? year, string? sortBy, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
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

        public Task<IEnumerable<Book>> SearchByAuthorAsync(string author)
        {
            throw new NotImplementedException();
        }

        public Task<Book?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetAvailableCopiesCountAsync(int bookId)
        {
            throw new NotImplementedException();
        }

        // Екранування спецсимволів LIKE, щоб '%' та '_' у назві шукались як звичайні символи
        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("%", @"\%", StringComparison.Ordinal)
                .Replace("_", @"\_", StringComparison.Ordinal);
        }
    }
}
