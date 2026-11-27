using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{
    // Репозиторій таблиці Libraries
    public class LibraryRepository : ILibraryRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public LibraryRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Для випадайок: усі філії, відсортовані за назвою
        public async Task<IReadOnlyList<Library>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Libraries
                .AsNoTracking()
                .OrderBy(library => library.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Library?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Libraries.AsNoTracking().FirstOrDefaultAsync(library => library.Id == id, cancellationToken);
        }

        // Фонд, use case 4 і 5: чи існує філія
        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Libraries.AnyAsync(library => library.Id == id, cancellationToken);
        }
        // Адміністрування, use case 1: чи є філія з такою назвою (без урахування регістру)
        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var namePattern = EscapeLikePattern(name);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Libraries
                .AnyAsync(library => EF.Functions.ILike(library.Name, namePattern), cancellationToken);
        }

        // Адміністрування, use case 1: додати філію, повертає її ідентифікатор
        public async Task<int> AddAsync(Library library, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            context.Libraries.Add(library);
            await context.SaveChangesAsync(cancellationToken);

            return library.Id;
        }

        // Екранування спецсимволів LIKE, щоб '%' та '_' у назві шукались як звичайні символи
        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("%", @"\%", StringComparison.Ordinal)
                .Replace("_", @"\_", StringComparison.Ordinal);
        }

        public async Task UpdateAsync(Library library, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            context.Libraries.Update(library);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
