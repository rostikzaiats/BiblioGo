using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Interfaces
{
    public interface ILoanRepository
    {
        // UC 1 (Читач): зберегти нову заявку зі статусом Requested
        Task<Loan> AddAsync(Loan loan);

        // UC 2 (Читач): позики користувача з опційним фільтром за статусом
        Task<IEnumerable<Loan>> GetByUserIdAsync(int userId, LoanStatus? status);

        // UC 4, 5: отримати позику за ідентифікатором
        Task<Loan?> GetByIdAsync(int loanId);

        // UC 4, 5: зберегти зміни існуючої позики (статус, дати)
        Task UpdateAsync(Loan loan);

        // UC 7 (Бібліотекар): журнал позик філії з фільтром, пагінацією
        Task<IEnumerable<Loan>> GetJournalAsync(int libraryId, LoanStatus? status, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize);

        // Допоміжне: чи має користувач активну заявку/позику на цю книгу
        Task<bool> HasActiveLoanForBookAsync(int userId, int bookId);
    }
}
