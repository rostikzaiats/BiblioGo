namespace BiblioGo.Application.Loans.Queries
{
    // UC 3 (Бібліотекар): Перевірити наявність примірника перед видачею
    public class CheckCopyAvailabilityQuery
    {
        public int BookCopyId { get; set; }
    }
}
