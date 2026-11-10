namespace BiblioGo.Application.Books.Validators
{
    // Спільна перевірка бібліографічних даних книги для «Додати нову книгу» та «Редагувати інформацію про книгу»
    public static class BookDataValidator
    {
        private const int MaxTitleLength = 200;
        private const int MaxAuthorLength = 100;
        private const int MaxGenreLength = 50;
        private const int MaxDescriptionLength = 2000;
        private const int MinYear = 1000;

        public static string? Validate(string title, string author, string genre, int year, string description)
        {
            return ValidateRequiredText(title, "Вкажіть назву книги", $"Назва книги не може бути довшою за {MaxTitleLength} символів", MaxTitleLength)
                ?? ValidateRequiredText(author, "Вкажіть автора книги", $"Ім'я автора не може бути довшим за {MaxAuthorLength} символів", MaxAuthorLength)
                ?? ValidateRequiredText(genre, "Вкажіть жанр книги", $"Жанр не може бути довшим за {MaxGenreLength} символів", MaxGenreLength)
                ?? ValidateYear(year)
                ?? ValidateDescription(description);
        }

        private static string? ValidateRequiredText(string value, string requiredMessage, string tooLongMessage, int maxLength)
        {
            var text = value.Trim();

            if (text.Length == 0)
            {
                return requiredMessage;
            }

            return text.Length > maxLength ? tooLongMessage : null;
        }

        private static string? ValidateYear(int year)
        {
            var maxYear = DateTime.Now.Year;

            return year >= MinYear && year <= maxYear
                ? null
                : $"Рік видання має бути від {MinYear} до {maxYear}";
        }

        private static string? ValidateDescription(string description)
        {
            return description.Trim().Length > MaxDescriptionLength
                ? $"Опис не може бути довшим за {MaxDescriptionLength} символів"
                : null;
        }
    }
}
