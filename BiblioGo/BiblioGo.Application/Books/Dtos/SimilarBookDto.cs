namespace BiblioGo.Application.Books.Dtos
{
    // Use case «Переглянути схожі книги»: книга того ж автора та/або жанру, що й обрана
    public class SimilarBookDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public bool SameAuthor { get; set; }
        public bool SameGenre { get; set; }

        // Пояснення для читача, чому книгу запропоновано
        public string Reason => (SameAuthor, SameGenre) switch
        {
            (true, true) => "Той самий автор і жанр",
            (true, false) => "Той самий автор",
            _ => "Той самий жанр"
        };
    }
}
