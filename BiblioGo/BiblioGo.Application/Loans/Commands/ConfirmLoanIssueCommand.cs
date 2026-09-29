using System;

namespace BiblioGo.Application.Loans.Commands
{
    // UC 4 (Бібліотекар): Підтвердити та оформити видачу книги
    public class ConfirmLoanIssueCommand
    {
        public int LoanId { get; set; }
        public int LibrarianId { get; set; }
        public DateTime DueDate { get; set; }
    }
}
