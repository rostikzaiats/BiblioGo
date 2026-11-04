using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Validators;
using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Books.Services
{
    // Сервіс підсистеми «Управління бібліотечним фондом»: валідація, бізнес-правила, логування, звернення до репозиторіїв.
    // Помилки (валідація, бізнес-правила, збій БД) повертаються як неуспішний Result, а не винятком
    public class BookCopyService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<BookCopyService> _logger;

        public BookCopyService(
            IBookRepository bookRepository,
            IBookCopyRepository bookCopyRepository,
            ILibraryRepository libraryRepository,
            ILogger<BookCopyService> logger)
        {
            _bookRepository = bookRepository;
            _bookCopyRepository = bookCopyRepository;
            _libraryRepository = libraryRepository;
            _logger = logger;
        }

        // Use case 1: додати нову книгу до каталогу (повертає Id створеної книги)
        public async Task<Result<int>> AddBookAsync(AddBookCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var validationError = AddBookValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Adding book rejected: {ValidationError}", validationError);
                return Result.Failure<int>(validationError);
            }

            var title = command.Title.Trim();
            var author = command.Author.Trim();

            _logger.LogInformation("Adding book {Title} by {Author}", title, author);

            try
            {
                if (await _bookRepository.ExistsAsync(title, author, cancellationToken))
                {
                    _logger.LogWarning("Adding book rejected: book {Title} by {Author} already exists", title, author);
                    return Result.Failure<int>("Книга з такою назвою та автором уже є в каталозі.");
                }

                var bookId = await _bookRepository.AddAsync(
                    new Book
                    {
                        Title = title,
                        Author = author,
                        Genre = command.Genre.Trim(),
                        Year = command.Year,
                        Description = command.Description.Trim()
                    },
                    cancellationToken);

                _logger.LogInformation("Added book {BookId} ({Title} by {Author})", bookId, title, author);
                return Result.Success(bookId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to add book {Title} by {Author}", title, author);
                return Result.Failure<int>("Не вдалося додати книгу. Спробуйте пізніше.");
            }
        }

        // Use case 4: додати фізичні примірники книги (повертає створені примірники з інвентарними номерами)
        public async Task<Result<IReadOnlyList<BookCopy>>> AddCopiesAsync(AddBookCopiesCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var validationError = AddBookCopiesValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Adding book copies rejected: {ValidationError}", validationError);
                return Result.Failure<IReadOnlyList<BookCopy>>(validationError);
            }

            _logger.LogInformation(
                "Adding {Quantity} copies of book {BookId} to library {LibraryId}",
                command.Quantity, command.BookId, command.LibraryId);

            try
            {
                if (await _bookRepository.GetByIdAsync(command.BookId, cancellationToken) is null)
                {
                    _logger.LogWarning("Adding book copies rejected: book {BookId} not found", command.BookId);
                    return Result.Failure<IReadOnlyList<BookCopy>>("Книгу не знайдено.");
                }

                if (!await _libraryRepository.ExistsAsync(command.LibraryId, cancellationToken))
                {
                    _logger.LogWarning("Adding book copies rejected: library {LibraryId} not found", command.LibraryId);
                    return Result.Failure<IReadOnlyList<BookCopy>>("Філію не знайдено.");
                }

                var copies = await _bookCopyRepository.AddCopiesAsync(
                    command.BookId, command.LibraryId, command.Quantity, cancellationToken);

                _logger.LogInformation(
                    "Added {Count} copies of book {BookId} to library {LibraryId}",
                    copies.Count, command.BookId, command.LibraryId);
                return Result.Success(copies);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex, "Failed to add copies of book {BookId} to library {LibraryId}",
                    command.BookId, command.LibraryId);
                return Result.Failure<IReadOnlyList<BookCopy>>("Не вдалося додати примірники. Спробуйте пізніше.");
            }
        }

        // Use case 5: переглянути кількість доступних та виданих книг (усі філії або лише одна)
        public async Task<Result<BookStockDto>> GetStockAsync(GetBookStockQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var validationError = GetBookStockValidator.Validate(query);
            if (validationError is not null)
            {
                _logger.LogWarning(
                    "Book stock view rejected: {ValidationError} (book {BookId}, library {LibraryId})",
                    validationError, query.BookId, query.LibraryId);
                return Result.Failure<BookStockDto>(validationError);
            }

            _logger.LogInformation("Loading stock of book {BookId} (library {LibraryId})", query.BookId, query.LibraryId);

            try
            {
                var book = await _bookRepository.GetByIdAsync(query.BookId, cancellationToken);
                if (book is null)
                {
                    _logger.LogWarning("Book stock view rejected: book {BookId} not found", query.BookId);
                    return Result.Failure<BookStockDto>("Книгу не знайдено.");
                }

                if (query.LibraryId is { } libraryId && !await _libraryRepository.ExistsAsync(libraryId, cancellationToken))
                {
                    _logger.LogWarning("Book stock view rejected: library {LibraryId} not found", libraryId);
                    return Result.Failure<BookStockDto>("Філію не знайдено.");
                }

                var counts = await _bookCopyRepository.CountByStatusAsync(query.BookId, query.LibraryId, cancellationToken);
                var stock = CreateStock(book, counts);

                _logger.LogInformation(
                    "Loaded stock of book {BookId}: {AvailableCopies} available, {IssuedCopies} issued, {WrittenOffCopies} written off",
                    query.BookId, stock.AvailableCopies, stock.IssuedCopies, stock.WrittenOffCopies);
                return Result.Success(stock);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load stock of book {BookId}", query.BookId);
                return Result.Failure<BookStockDto>("Не вдалося завантажити залишки книги. Спробуйте пізніше.");
            }
        }

        // Use case 6: списати примірник (виданий або вже списаний примірник списати не можна)
        public async Task<Result> WriteOffAsync(WriteOffBookCopyCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var validationError = WriteOffBookCopyValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Write-off of book copy {BookCopyId} rejected: {ValidationError}", command.BookCopyId, validationError);
                return Result.Failure(validationError);
            }

            _logger.LogInformation("Writing off book copy {BookCopyId}", command.BookCopyId);

            try
            {
                var bookCopy = await _bookCopyRepository.GetByIdAsync(command.BookCopyId, cancellationToken);

                if (bookCopy is null)
                {
                    _logger.LogWarning("Write-off rejected: book copy {BookCopyId} not found", command.BookCopyId);
                    return Result.Failure("Примірник не знайдено.");
                }

                var businessRuleError = GetWriteOffRestriction(bookCopy.Status);

                if (businessRuleError is not null)
                {
                    _logger.LogWarning(
                        "Write-off of book copy {BookCopyId} rejected: status is {Status}",
                        command.BookCopyId,
                        bookCopy.Status);
                    return Result.Failure(businessRuleError);
                }

                if (!await _bookCopyRepository.SetStatusAsync(command.BookCopyId, BookCopyStatus.WrittenOff, cancellationToken))
                {
                    _logger.LogWarning("Write-off rejected: book copy {BookCopyId} disappeared before the status change", command.BookCopyId);
                    return Result.Failure("Примірник не знайдено.");
                }

                _logger.LogInformation("Book copy {BookCopyId} written off", command.BookCopyId);
                return Result.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to write off book copy {BookCopyId}", command.BookCopyId);
                return Result.Failure("Не вдалося списати примірник. Спробуйте пізніше.");
            }
        }

        // Усього = примірники в обігу (на полицях і на руках); списані до загальної кількості не входять
        private static BookStockDto CreateStock(Book book, IReadOnlyDictionary<BookCopyStatus, int> counts)
        {
            var available = counts.GetValueOrDefault(BookCopyStatus.Available);
            var issued = counts.GetValueOrDefault(BookCopyStatus.Issued);

            return new BookStockDto
            {
                BookId = book.Id,
                Title = book.Title,
                Author = book.Author,
                TotalCopies = available + issued,
                AvailableCopies = available,
                IssuedCopies = issued,
                WrittenOffCopies = counts.GetValueOrDefault(BookCopyStatus.WrittenOff)
            };
        }

        private static string? GetWriteOffRestriction(BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Issued => "Не можна списати виданий примірник. Спочатку оформіть його повернення.",
                BookCopyStatus.WrittenOff => "Примірник уже списано.",
                _ => null
            };
        }
    }
}
