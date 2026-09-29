namespace BiblioGo.Application.Books.Commands
{
    // Use case 4: додати фізичні примірники книги (інвентарні номери генеруються системою)
    public class AddBookCopiesCommand
    {
        public int BookId { get; set; }
        public int LibraryId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
