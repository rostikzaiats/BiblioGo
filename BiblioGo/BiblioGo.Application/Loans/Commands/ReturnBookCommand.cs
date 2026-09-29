namespace BiblioGo.Application.Loans.Commands
{
    // UC 5 (Бібліотекар): Оформити повернення книги
    public class ReturnBookCommand
    {
        public int LoanId { get; set; }
        public int LibrarianId { get; set; }
    }
}
