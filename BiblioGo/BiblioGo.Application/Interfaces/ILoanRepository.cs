
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

        // UC 7 (Бібліотекар): сторінка журналу позик філії (разом із читачем, книгою та примірником), нові першими.
        // issuedFrom — включно, issuedTo — виключно (обидва UTC, фільтр за датою оформлення), skip/take — пагінація
        Task<IReadOnlyList<Loan>> GetJournalPageAsync(int libraryId, LoanStatus? status, DateTime? issuedFrom, DateTime? issuedTo, int skip, int take, CancellationToken cancellationToken = default);

        // UC 7: кількість позик філії під тими самими фільтрами
        Task<int> CountJournalAsync(int libraryId, LoanStatus? status, DateTime? issuedFrom, DateTime? issuedTo, CancellationToken cancellationToken = default);

        // UC 1: чи має користувач активну (Requested або Issued) заявку/позику на цю книгу
        Task<bool> HasActiveLoanForBookAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    }
}
