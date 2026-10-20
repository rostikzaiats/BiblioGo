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

        // Use case 2: позики користувача разом із книгою та філією примірника, нові першими
        public async Task<IReadOnlyList<Loan>> GetByUserIdAsync(int userId, LoanStatus? status, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var query = context.Loans
                .AsNoTracking()
                .Include(loan => loan.BookCopy).ThenInclude(copy => copy.Book)
                .Include(loan => loan.BookCopy).ThenInclude(copy => copy.Library)
                .Where(loan => loan.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(loan => loan.Status == status.Value);
            }

            return await query
                .OrderByDescending(loan => loan.IssueDate)
                .ThenByDescending(loan => loan.Id)
                .ToListAsync(cancellationToken);
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
