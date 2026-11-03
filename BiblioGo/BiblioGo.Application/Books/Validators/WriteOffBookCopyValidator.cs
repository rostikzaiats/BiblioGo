using BiblioGo.Application.Books.Commands;

namespace BiblioGo.Application.Books.Validators
{
    public static class WriteOffBookCopyValidator
    {
        public static string? Validate(WriteOffBookCopyCommand command)
        {
            return command.BookCopyId > 0 ? null : "Оберіть примірник";
        }
    }
}
