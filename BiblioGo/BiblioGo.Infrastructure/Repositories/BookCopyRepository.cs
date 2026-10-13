using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{
    // Репозиторій таблиці BookCopies
    public class BookCopyRepository : IBookCopyRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public BookCopyRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // UC 1, 3, 4, 6: отримати примірник за ідентифікатором
        public async Task<BookCopy?> GetByIdAsync(int bookCopyId, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.BookCopies
                .AsNoTracking()
                .FirstOrDefaultAsync(copy => copy.Id == bookCopyId, cancellationToken);
        }

        // Use case 1: вільний примірник книги у філії. Примірник лишається Available до підтвердження видачі,
        // тому виключаємо ті, на які вже подано заявку Requested, щоб двом читачам не дістався один примірник
        public async Task<BookCopy?> FindAvailableCopyAsync(int bookId, int libraryId, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.BookCopies
                .AsNoTracking()
                .Where(copy => copy.BookId == bookId
                    && copy.LibraryId == libraryId
                    && copy.Status == BookCopyStatus.Available
                    && !context.Loans.Any(loan => loan.BookCopyId == copy.Id && loan.Status == LoanStatus.Requested))
                .OrderBy(copy => copy.InventoryNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> IsAvailableAsync(int bookCopyId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(BookCopy bookCopy)
        {
            throw new NotImplementedException();
        }

        // UC 6: змінити статус примірника (один UPDATE без попереднього читання запису)
        public async Task SetStatusAsync(int bookCopyId, BookCopyStatus status, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var updatedRows = await context.BookCopies
                .Where(copy => copy.Id == bookCopyId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(copy => copy.Status, status), cancellationToken);

            if (updatedRows == 0)
            {
                throw new NotFoundException($"Примірник {bookCopyId} не знайдено.");
            }
        }
    }
}