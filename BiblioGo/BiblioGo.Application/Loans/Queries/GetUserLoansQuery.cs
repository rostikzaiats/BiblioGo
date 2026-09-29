using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Queries
{
    // UC 2 (Читач): Переглянути власні позики та терміни
    public class GetUserLoansQuery
    {
        public int UserId { get; set; }
        public LoanStatus? Status { get; set; }
    }
}
