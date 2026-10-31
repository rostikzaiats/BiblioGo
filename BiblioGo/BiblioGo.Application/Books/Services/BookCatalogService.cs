using System.Globalization;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
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
        public const int MaxCatalogPageSize = 50;
        public const int MaxCatalogPageNumber = 100_000;
        public const int MaxCatalogGenreLength = 50;
        public const int MinCatalogYear = 1000;

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

                var loanableCopies = await _bookCopyRepository.GetLoanableByBookAsync(bookId, cancellationToken);
                var issuedCopies = await _bookCopyRepository.CountIssuedByBookAsync(bookId, cancellationToken);
                var details = CreateDetails(book, loanableCopies, issuedCopies);

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

        // Групуємо вільні примірники за філіями (філії з найбільшою кількістю — першими)
        private static BookDetailsDto CreateDetails(Book book, IReadOnlyList<BookCopy> availableCopies, int issuedCopies)
        {
            return new BookDetailsDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Year = book.Year,
                Description = book.Description,
                AvailableCopies = availableCopies.Count,
                IssuedCopies = issuedCopies,
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

        // Use case 1: переглянути каталог книг (фільтр за жанром і роком, сортування, сторінки)
        public async Task<Result<CatalogPageDto>> GetCatalogAsync(GetCatalogQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var genre = string.IsNullOrWhiteSpace(query.Genre) ? null : query.Genre.Trim();
            var validationError = ValidateCatalogQuery(query, genre, out var year);

            if (validationError is not null)
            {
                _logger.LogWarning(
                    "Catalog view rejected: {ValidationError} (page {PageNumber}, size {PageSize}, genre {Genre}, year {Year}, sort {SortBy})",
                    validationError,
                    query.PageNumber,
                    query.PageSize,
                    query.Genre,
                    query.Year,
                    query.SortBy);
                return Result.Failure<CatalogPageDto>(validationError);
            }

            _logger.LogInformation(
                "Loading catalog page {PageNumber} (genre {Genre}, year {Year}, sort {SortBy})",
                query.PageNumber,
                genre,
                year,
                query.SortBy);

            try
            {
                var totalCount = await _bookRepository.CountCatalogAsync(genre, year, cancellationToken);
                var skip = (query.PageNumber - 1) * query.PageSize;
                var books = await _bookRepository.GetCatalogPageAsync(genre, year, query.SortBy, skip, query.PageSize, cancellationToken);

                _logger.LogInformation(
                    "Loaded catalog page {PageNumber}: {BookCount} of {TotalCount} book(s)",
                    query.PageNumber,
                    books.Count,
                    totalCount);

                return Result.Success(new CatalogPageDto
                {
                    Books = books,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (totalCount + query.PageSize - 1) / query.PageSize
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load catalog page {PageNumber}", query.PageNumber);
                return Result.Failure<CatalogPageDto>("Не вдалося завантажити каталог. Спробуйте пізніше.");
            }
        }

        // Use case 1: жанри для випадайки фільтра каталогу
        public async Task<Result<IReadOnlyList<string>>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var genres = await _bookRepository.GetGenresAsync(cancellationToken);

                _logger.LogInformation("Loaded {GenreCount} genre(s) for catalog filter", genres.Count);
                return Result.Success(genres);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load genres for catalog filter");
                return Result.Failure<IReadOnlyList<string>>("Не вдалося завантажити список жанрів. Спробуйте пізніше.");
            }
        }

        private static string? ValidateCatalogQuery(GetCatalogQuery query, string? genre, out int? year)
        {
            year = null;

            if (!Enum.IsDefined(query.SortBy))
            {
                return "Оберіть спосіб сортування.";
            }

            if (query.PageNumber < 1 || query.PageNumber > MaxCatalogPageNumber)
            {
                return $"Номер сторінки має бути від 1 до {MaxCatalogPageNumber}.";
            }

            if (query.PageSize < 1 || query.PageSize > MaxCatalogPageSize)
            {
                return $"Кількість книг на сторінці має бути від 1 до {MaxCatalogPageSize}.";
            }

            if (genre is not null && genre.Length > MaxCatalogGenreLength)
            {
                return $"Жанр не може бути довшим за {MaxCatalogGenreLength} символів.";
            }

            var yearText = query.Year?.Trim();
            if (string.IsNullOrEmpty(yearText))
            {
                return null;
            }

            var maxYear = DateTime.Now.Year;
            if (!int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear))
            {
                return "Рік має бути цілим числом, наприклад 1840.";
            }

            if (parsedYear < MinCatalogYear || parsedYear > maxYear)
            {
                return $"Рік має бути від {MinCatalogYear} до {maxYear}.";
            }

            year = parsedYear;
            return null;
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
