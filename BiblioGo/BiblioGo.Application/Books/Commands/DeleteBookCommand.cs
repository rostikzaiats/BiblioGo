namespace BiblioGo.Application.Books.Commands
{
    // Use case 3: видалити книгу з каталогу (лише за відсутності активних позик)
    public class DeleteBookCommand
    {
        public int BookId { get; set; }
    }
}
