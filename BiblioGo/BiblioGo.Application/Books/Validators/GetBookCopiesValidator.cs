using BiblioGo.Application.Books.Queries;

namespace BiblioGo.Application.Books.Validators
{
    public static class GetBookCopiesValidator
    {
        public static string? Validate(GetBookCopiesQuery query)
        {
            if (query.BookId <= 0)
            {
                return "Оберіть книгу";
            }

            // Філія і статус необов'язкові (null — без фільтра), але якщо вказані, мають бути коректними
            if (query.LibraryId is <= 0)
            {
                return "Оберіть філію бібліотеки";
            }

            return query.Status is { } status && !Enum.IsDefined(status) ? "Оберіть статус примірника" : null;
        }
    }
}
