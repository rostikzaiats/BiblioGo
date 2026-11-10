using BiblioGo.Application.Books.Commands;

namespace BiblioGo.Application.Books.Validators
{
    public static class UpdateBookValidator
    {
        public static string? Validate(UpdateBookCommand command)
        {
            if (command.BookId <= 0)
            {
                return "Оберіть книгу";
            }

            return BookDataValidator.Validate(command.Title, command.Author, command.Genre, command.Year, command.Description);
        }
    }
}
