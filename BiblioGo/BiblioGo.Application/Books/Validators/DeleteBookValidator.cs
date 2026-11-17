using BiblioGo.Application.Books.Commands;

namespace BiblioGo.Application.Books.Validators
{
    public static class DeleteBookValidator
    {
        public static string? Validate(DeleteBookCommand command)
        {
            return command.BookId > 0 ? null : "Оберіть книгу";
        }
    }
}
