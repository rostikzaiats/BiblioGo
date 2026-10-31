using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Books.Services
{
    // Сервіс підсистеми «Каталог та пошук книг»: валідація вхідних даних, логування, звернення до репозиторію.
    // Помилка валідації повертається як неуспішний Result, а не виняток
    public class BookCatalogService
    {
        public const int MinSearchTitleLength = 2;
        public const int MaxSearchTitleLength = 200;
        public const int MinSearchAuthorLength = 2;
        public const int MaxSearchAuthorLength = 100;

        private readonly IBookRepository _bookRepository;
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly ILogger<BookCatalogService> _logger;

        public BookCatalogService(
            IBookRepository bookRepository,
            IBookCopyRepository bookCopyRepository,
            ILogger<BookCatalogService> logger)
        {
            _bookRepository = bookRepository;
            _bookCopyRepository = bookCopyRepository;
            _logger = logger;
        }

        // Use case 2: знайти книгу за назвою
        public async Task<Result<IReadOnlyList<Book>>> SearchByTitleAsync(string? title, CancellationToken cancellationToken = default)
        {
            var searchTitle = title?.Trim() ?? string.Empty;
            var validationError = ValidateSearchTitle(searchTitle);

            if (validationError is not null)
            {
                _logger.LogWarning("Book search by title {Title} rejected: {ValidationError}", title, validationError);
                return Result.Failure<IReadOnlyList<Book>>(validationError);
            }

            _logger.LogInformation("Searching books by title {Title}", searchTitle);

            try
            {
                var books = await _bookRepository.SearchByTitleAsync(searchTitle, cancellationToken);

                _logger.LogInformation("Found {BookCount} book(s) by title {Title}", books.Count, searchTitle);

                return Result.Success(books);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to search books by title {Title}", searchTitle);
                throw;
            }
        }

        // Use case 3: знайти книгу за автором
        public async Task<Result<IReadOnlyList<Book>>> SearchByAuthorAsync(string? author, CancellationToken cancellationToken = default)
        {
            var searchAuthor = author?.Trim() ?? string.Empty;
            var validationError = ValidateSearchAuthor(searchAuthor);

            if (validationError is not null)
            {
                _logger.LogWarning("Book search by author {Author} rejected: {ValidationError}", author, validationError);
                return Result.Failure<IReadOnlyList<Book>>(validationError);
            }

            _logger.LogInformation("Searching books by author {Author}", searchAuthor);

            try
            {
                var books = await _bookRepository.SearchByAuthorAsync(searchAuthor, cancellationToken);

                _logger.LogInformation("Found {BookCount} book(s) by author {Author}", books.Count, searchAuthor);

                return Result.Success(books);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to search books by author {Author}", searchAuthor);
                throw;
            }
        }

        // Use case 8: переглянути інформацію про книгу (опис + де і скільки вільних примірників)
        public async Task<Result<BookDetailsDto>> GetBookDetailsAsync(int bookId, CancellationToken cancellationToken = default)
        {
            if (bookId <= 0)
            {
                const string validationError = "Оберіть книгу.";
                _logger.LogWarning("Book details for {BookId} rejected: {ValidationError}", bookId, validationError);
                return Result.Failure<BookDetailsDto>(validationError);
            }

            _logger.LogInformation("Loading details of book {BookId}", bookId);

            try
            {
                var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);

                if (book is null)
                {
                    _logger.LogWarning("Book details rejected: book {BookId} not found", bookId);
                    return Result.Failure<BookDetailsDto>("Книгу не знайдено.");
                }

                var copies = await _bookCopyRepository.GetNotWrittenOffByBookAsync(bookId, cancellationToken);
                var details = CreateDetails(book, copies);

                _logger.LogInformation(
                    "Loaded details of book {BookId}: {AvailableCopies} available, {IssuedCopies} issued",
                    bookId,
                    details.AvailableCopies,
                    details.IssuedCopies);

                return Result.Success(details);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load details of book {BookId}", bookId);
                return Result.Failure<BookDetailsDto>("Не вдалося завантажити інформацію про книгу. Спробуйте пізніше.");
            }
        }

        // Рахуємо вільні й видані примірники та групуємо вільні за філіями (філії з найбільшою кількістю — першими)
        private static BookDetailsDto CreateDetails(Book book, IReadOnlyList<BookCopy> copies)
        {
            var availableCopies = copies.Where(copy => copy.Status == BookCopyStatus.Available).ToList();

            return new BookDetailsDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Year = book.Year,
                Description = book.Description,
                AvailableCopies = availableCopies.Count,
                IssuedCopies = copies.Count(copy => copy.Status == BookCopyStatus.Issued),
                AvailableInLibraries = availableCopies
                    .GroupBy(copy => copy.LibraryId)
                    .Select(group => new LibraryAvailabilityDto
                    {
                        LibraryName = group.First().Library.Name,
                        Address = group.First().Library.Address,
                        AvailableCopies = group.Count()
                    })
                    .OrderByDescending(library => library.AvailableCopies)
                    .ThenBy(library => library.LibraryName, StringComparer.CurrentCulture)
                    .ToList()
            };
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
