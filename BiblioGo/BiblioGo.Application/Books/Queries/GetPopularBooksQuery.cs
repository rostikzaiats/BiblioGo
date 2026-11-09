namespace BiblioGo.Application.Books.Queries
{
    // Use case «Популярні книги»: період і скільки книг показати в рейтингу
    public class GetPopularBooksQuery
    {
        public PopularBooksPeriod Period { get; set; } = PopularBooksPeriod.AllTime;
        public int Top { get; set; } = 10;
    }
}
