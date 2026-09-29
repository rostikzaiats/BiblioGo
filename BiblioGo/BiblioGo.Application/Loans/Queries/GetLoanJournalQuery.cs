using System;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Queries
{
    // UC 7 (Бібліотекар): Переглянути журнал позик
    public class GetLoanJournalQuery
    {
        public int LibraryId { get; set; }
        public LoanStatus? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
