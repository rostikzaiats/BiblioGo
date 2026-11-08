namespace BiblioGo.Application.Books.Dtos
{
    // Use case «Популярні книги»: рядок рейтингу. Rank виставляє сервіс, решту повертає репозиторій
    public class PopularBookDto
    {
        public int Rank { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public int IssueCount { get; set; }
    }
}
