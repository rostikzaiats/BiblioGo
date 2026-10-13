using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{
    // Репозиторій таблиці Loans
    public class LoanRepository : ILoanRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public LoanRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Use case 1: зберегти нову заявку
        public async Task<Loan> AddAsync(Loan loan, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            context.Loans.Add(loan);
            await context.SaveChangesAsync(cancellationToken);

            return loan;
        }

        // Use case 1: чи є в користувача незакрита заявка (Requested) або позика (Issued) на цю книгу
        public async Task<bool> HasActiveLoanForBookAsync(int userId, int bookId, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Loans
                .AsNoTracking()
                .AnyAsync(
                    loan => loan.UserId == userId
                        && loan.BookCopy.BookId == bookId
                        && (loan.Status == LoanStatus.Requested || loan.Status == LoanStatus.Issued),
                    cancellationToken);
        }

        public Task<IEnumerable<Loan>> GetByUserIdAsync(int userId, LoanStatus? status)
        {
            throw new NotImplementedException();
        }

        public Task<Loan?> GetByIdAsync(int loanId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Loan loan)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Loan>> GetJournalAsync(int libraryId, LoanStatus? status, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }
    }
}
