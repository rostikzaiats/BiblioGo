namespace BiblioGo.Application.Books.Dtos
{
    // Use case 5: залишки по книзі (на полицях / на руках / списані)
    public class BookStockDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int IssuedCopies { get; set; }
        public int WrittenOffCopies { get; set; }
    }
}
