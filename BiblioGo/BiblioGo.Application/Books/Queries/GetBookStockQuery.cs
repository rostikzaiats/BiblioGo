namespace BiblioGo.Application.Books.Queries
{
    // Use case 5: переглянути кількість доступних та виданих книг
    public class GetBookStockQuery
    {
        public int BookId { get; set; }
        public int? LibraryId { get; set; }
    }
}
