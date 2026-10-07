namespace BiblioGo.Application.Books.Commands
{
    // Use case 1: додати нову книгу до каталогу
    public class AddBookCommand
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
