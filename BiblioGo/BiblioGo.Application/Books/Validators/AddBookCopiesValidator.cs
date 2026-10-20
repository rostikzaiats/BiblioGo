using BiblioGo.Application.Books.Commands;

namespace BiblioGo.Application.Books.Validators
{
    public static class AddBookCopiesValidator
    {
        private const int MinQuantity = 1;
        private const int MaxQuantity = 100;

        public static string? Validate(AddBookCopiesCommand command)
        {
            return ValidateId(command.BookId, "Оберіть книгу")
                ?? ValidateId(command.LibraryId, "Оберіть філію бібліотеки")
                ?? ValidateQuantity(command.Quantity);
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }

        private static string? ValidateQuantity(int quantity)
        {
            return quantity is >= MinQuantity and <= MaxQuantity
                ? null
                : $"Кількість примірників має бути від {MinQuantity} до {MaxQuantity}";
        }
    }
}
