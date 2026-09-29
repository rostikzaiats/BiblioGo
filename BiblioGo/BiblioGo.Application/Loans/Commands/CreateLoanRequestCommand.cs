namespace BiblioGo.Application.Loans.Commands
{
    // UC 1 (Читач): Оформити заявку на отримання книги
    public class CreateLoanRequestCommand
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int LibraryId { get; set; }
    }
}
