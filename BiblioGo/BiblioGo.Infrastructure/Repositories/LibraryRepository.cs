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
    }
}
