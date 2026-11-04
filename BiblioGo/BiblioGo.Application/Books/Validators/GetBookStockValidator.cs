using BiblioGo.Application.Books.Queries;

namespace BiblioGo.Application.Books.Validators
{
    public static class GetBookStockValidator
    {
        public static string? Validate(GetBookStockQuery query)
        {
            if (query.BookId <= 0)
            {
                return "Оберіть книгу";
            }

            // Філія необов'язкова (null — усі філії), але якщо вказана, має бути коректною
            return query.LibraryId is <= 0 ? "Оберіть філію бібліотеки" : null;
        }
    }
}
