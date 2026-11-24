namespace BiblioGo.Application.Books.Queries
{
    // Use case «Переглянути схожі книги»: для якої книги шукаємо схожі і скільки показати
    public class GetSimilarBooksQuery
    {
        public int BookId { get; set; }
        public int Top { get; set; } = 10;
    }
}
