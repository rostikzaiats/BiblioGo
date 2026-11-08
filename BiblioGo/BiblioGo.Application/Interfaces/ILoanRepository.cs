
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Interfaces
{
    public interface ILoanRepository
    {
        // UC 1 (Читач): зберегти нову заявку зі статусом Requested
        Task<Loan> AddAsync(Loan loan, CancellationToken cancellationToken = default);

        // UC 2 (Читач): позики користувача (з книгою та філією) з опційним фільтром за статусом, нові першими
        Task<IReadOnlyList<Loan>> GetByUserIdAsync(int userId, LoanStatus? status, CancellationToken cancellationToken = default);

        // UC 4, 5: отримати позику за ідентифікатором
        Task<Loan?> GetByIdAsync(int loanId, CancellationToken cancellationToken = default);

        // UC 4: в одній транзакції позика Requested -> Issued (дати видачі та повернення) і примірник
        // Available -> Issued. false — якщо за цей час стан позики або примірника змінився (нічого не збережено)
        Task<bool> ConfirmIssueAsync(int loanId, int bookCopyId, DateTime issueDate, DateTime dueDate, CancellationToken cancellationToken = default);

        // UC 5: в одній транзакції позика Issued -> Returned (дата повернення) і примірник Issued -> Available.
        // false — якщо за цей час стан позики або примірника змінився (нічого не збережено)
        Task<bool> ReturnAsync(int loanId, int bookCopyId, DateTime returnDate, CancellationToken cancellationToken = default);

        // UC 4, 5: зберегти зміни існуючої позики (статус, дати)
        Task UpdateAsync(Loan loan);

        // UC 7 (Бібліотекар): журнал позик філії з фільтром, пагінацією
        Task<IEnumerable<Loan>> GetJournalAsync(int libraryId, LoanStatus? status, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize);

        // UC 1: чи має користувач активну (Requested або Issued) заявку/позику на цю книгу
        Task<bool> HasActiveLoanForBookAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    }
}
