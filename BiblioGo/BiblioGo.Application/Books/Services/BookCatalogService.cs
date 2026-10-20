using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Books.Services
{
    // Сервіс підсистеми «Каталог та пошук книг»: валідація вхідних даних, логування, звернення до репозиторію
    public class BookCatalogService
    {
        public const int MinSearchTitleLength = 2;
        public const int MaxSearchTitleLength = 200;
        public const int MinSearchAuthorLength = 2;
        public const int MaxSearchAuthorLength = 100;

        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BookCatalogService> _logger;

        public BookCatalogService(IBookRepository bookRepository, ILogger<BookCatalogService> logger)
        {
            _bookRepository = bookRepository;
            _logger = logger;
        }

        // Use case 2: знайти книгу за назвою
        public async Task<IReadOnlyList<Book>> SearchByTitleAsync(string? title, CancellationToken cancellationToken = default)
        {
            var searchTitle = title?.Trim() ?? string.Empty;
            var validationError = ValidateSearchTitle(searchTitle);

            if (validationError is not null)
            {
                _logger.LogWarning("Book search by title {Title} rejected: {ValidationError}", title, validationError);
                throw new InputValidationException(validationError);
            }

            _logger.LogInformation("Searching books by title {Title}", searchTitle);

            try
            {
                var books = await _bookRepository.SearchByTitleAsync(searchTitle, cancellationToken);

                _logger.LogInformation("Found {BookCount} book(s) by title {Title}", books.Count, searchTitle);

                return books;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to search books by title {Title}", searchTitle);
                throw;
            }
        }

        // Use case 3: знайти книгу за автором
        public async Task<IReadOnlyList<Book>> SearchByAuthorAsync(string? author, CancellationToken cancellationToken = default)
        {
            var searchAuthor = author?.Trim() ?? string.Empty;
            var validationError = ValidateSearchAuthor(searchAuthor);

            if (validationError is not null)
            {
                _logger.LogWarning("Book search by author {Author} rejected: {ValidationError}", author, validationError);
                throw new InputValidationException(validationError);
            }

            _logger.LogInformation("Searching books by author {Author}", searchAuthor);

            try
            {
                var books = await _bookRepository.SearchByAuthorAsync(searchAuthor, cancellationToken);

                _logger.LogInformation("Found {BookCount} book(s) by author {Author}", books.Count, searchAuthor);

                return books;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to search books by author {Author}", searchAuthor);
                throw;
            }
        }

        private static string? ValidateSearchTitle(string searchTitle)
        {
            if (searchTitle.Length == 0)
            {
                return "Введіть назву книги для пошуку.";
            }

            if (searchTitle.Length < MinSearchTitleLength)
            {
                return $"Назва для пошуку має містити щонайменше {MinSearchTitleLength} символи.";
            }

            if (searchTitle.Length > MaxSearchTitleLength)
            {
                return $"Назва для пошуку не може бути довшою за {MaxSearchTitleLength} символів.";
            }

            return null;
        }

        private static string? ValidateSearchAuthor(string searchAuthor)
        {
            if (searchAuthor.Length == 0)
            {
                return "Введіть ім'я або прізвище автора для пошуку.";
            }

            if (searchAuthor.Length < MinSearchAuthorLength)
            {
                return $"Ім'я автора для пошуку має містити щонайменше {MinSearchAuthorLength} символи.";
            }

            if (searchAuthor.Length > MaxSearchAuthorLength)
            {
                return $"Ім'я автора для пошуку не може бути довшим за {MaxSearchAuthorLength} символів.";
            }

            return null;
        }
    }
}
