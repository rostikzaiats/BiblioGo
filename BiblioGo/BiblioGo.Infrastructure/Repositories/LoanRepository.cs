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

        // Use case 4, 5: позика за ідентифікатором
        public async Task<Loan?> GetByIdAsync(int loanId, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Loans
                .AsNoTracking()
                .FirstOrDefaultAsync(loan => loan.Id == loanId, cancellationToken);
        }

        // Use case 4: видача. Умовні UPDATE у транзакції не дають підтвердити заявку, яка вже змінилась, або видати
        // примірник двічі; якщо хоч одна умова не виконалась — транзакція відкочується (Dispose без Commit)
        public async Task<bool> ConfirmIssueAsync(int loanId, int bookCopyId, DateTime issueDate, DateTime dueDate, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var updatedLoans = await context.Loans
                .Where(loan => loan.Id == loanId && loan.Status == LoanStatus.Requested)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(loan => loan.Status, LoanStatus.Issued)
                        .SetProperty(loan => loan.IssueDate, issueDate)
                        .SetProperty(loan => loan.DueDate, dueDate),
                    cancellationToken);

            if (updatedLoans == 0)
            {
                return false;
            }

            var updatedCopies = await context.BookCopies
                .Where(copy => copy.Id == bookCopyId && copy.Status == BookCopyStatus.Available)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(copy => copy.Status, BookCopyStatus.Issued),
                    cancellationToken);

            if (updatedCopies == 0)
            {
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        // Use case 5: повернення, та сама схема, що й у ConfirmIssueAsync
        public async Task<bool> ReturnAsync(int loanId, int bookCopyId, DateTime returnDate, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var updatedLoans = await context.Loans
                .Where(loan => loan.Id == loanId && loan.Status == LoanStatus.Issued)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(loan => loan.Status, LoanStatus.Returned)
                        .SetProperty(loan => loan.ReturnDate, (DateTime?)returnDate),
                    cancellationToken);

            if (updatedLoans == 0)
            {
                return false;
            }

            var updatedCopies = await context.BookCopies
                .Where(copy => copy.Id == bookCopyId && copy.Status == BookCopyStatus.Issued)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(copy => copy.Status, BookCopyStatus.Available),
                    cancellationToken);

            if (updatedCopies == 0)
            {
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public Task UpdateAsync(Loan loan)
        {
            throw new NotImplementedException();
        }

        // Use case 7: сторінка журналу філії. Сортування має другий ключ (Id), щоб порядок між сторінками був стабільним
        public async Task<IReadOnlyList<Loan>> GetJournalPageAsync(
            int libraryId,
            LoanStatus? status,
            DateTime? issuedFrom,
            DateTime? issuedTo,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await ApplyJournalFilters(context.Loans.AsNoTracking(), libraryId, status, issuedFrom, issuedTo)
                .Include(loan => loan.User)
                .Include(loan => loan.BookCopy).ThenInclude(copy => copy.Book)
                .OrderByDescending(loan => loan.IssueDate)
                .ThenByDescending(loan => loan.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        // Use case 7: кількість позик філії під фільтрами
        public async Task<int> CountJournalAsync(
            int libraryId,
            LoanStatus? status,
            DateTime? issuedFrom,
            DateTime? issuedTo,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await ApplyJournalFilters(context.Loans, libraryId, status, issuedFrom, issuedTo)
                .CountAsync(cancellationToken);
        }

        // Філія позики — філія її примірника; дата — дата оформлення (для заявки це час її подання)
        private static IQueryable<Loan> ApplyJournalFilters(
            IQueryable<Loan> loans,
            int libraryId,
            LoanStatus? status,
            DateTime? issuedFrom,
            DateTime? issuedTo)
        {
            loans = loans.Where(loan => loan.BookCopy.LibraryId == libraryId);

            if (status.HasValue)
            {
                loans = loans.Where(loan => loan.Status == status.Value);
            }

            if (issuedFrom.HasValue)
            {
                loans = loans.Where(loan => loan.IssueDate >= issuedFrom.Value);
            }

            if (issuedTo.HasValue)
            {
                loans = loans.Where(loan => loan.IssueDate < issuedTo.Value);
            }

            return loans;
        }
    }
}
