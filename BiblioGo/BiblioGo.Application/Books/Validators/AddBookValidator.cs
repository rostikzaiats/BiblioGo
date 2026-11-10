using BiblioGo.Application.Books.Commands;

namespace BiblioGo.Application.Books.Validators
{
    public static class AddBookValidator
    {
        public static string? Validate(AddBookCommand command)
        {
            return BookDataValidator.Validate(command.Title, command.Author, command.Genre, command.Year, command.Description);
        }
    }
}
