namespace BiblioGo.Application.Books.Queries
{
    // Use case 6 (допоміжний): список фізичних примірників книги з інвентарними номерами та статусами
    public class GetBookCopiesQuery
    {
        public int BookId { get; set; }
        public int? LibraryId { get; set; }
    }
}
