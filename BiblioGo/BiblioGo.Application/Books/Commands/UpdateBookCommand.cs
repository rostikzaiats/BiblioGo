namespace BiblioGo.Application.Books.Commands
{
    // Use case 2: редагувати інформацію про книгу
    public class UpdateBookCommand
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
