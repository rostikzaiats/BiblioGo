namespace BiblioGo.Application.Loans.Queries
{
    // UC 3 (Бібліотекар): перевірити наявність примірника перед видачею.
    // Бібліотекар вводить номер заявки, система перевіряє закріплений за нею примірник
    public class CheckCopyAvailabilityQuery
    {
        public int LoanId { get; set; }
        public int LibrarianId { get; set; }
    }
}
