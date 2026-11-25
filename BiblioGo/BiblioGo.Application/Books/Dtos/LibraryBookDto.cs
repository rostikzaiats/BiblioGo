namespace BiblioGo.Application.Books.Dtos
{
    // Use case «Переглянути книги, доступні в бібліотеці»: книга і скільки її вільних примірників у обраній філії
    public class LibraryBookDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public int AvailableCopies { get; set; }
    }
}
