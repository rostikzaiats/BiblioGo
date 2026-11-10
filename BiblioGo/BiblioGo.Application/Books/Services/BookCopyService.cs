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
    // Сервіс підсистеми «Управління бібліотечним фондом»: валідація, бізнес-правила, логування відхилень, звернення до репозиторіїв.
    // Помилки валідації та порушення бізнес-правил повертаються як неуспішний Result, а не винятком
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
            var validationError = AddBookValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Adding book rejected: {ValidationError}", validationError);
                return Result.Failure<int>(validationError);
            }

            var title = command.Title.Trim();
            var author = command.Author.Trim();

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

            return Result.Success(bookId);
        }

        // Use case 2: редагувати інформацію про книгу
        public async Task<Result> UpdateBookAsync(UpdateBookCommand command, CancellationToken cancellationToken = default)
        {
            var validationError = UpdateBookValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Updating book {BookId} rejected: {ValidationError}", command.BookId, validationError);
                return Result.Failure(validationError);
            }

            var title = command.Title.Trim();
            var author = command.Author.Trim();

            if (await _bookRepository.ExistsAnotherAsync(title, author, command.BookId, cancellationToken))
            {
                _logger.LogWarning(
                    "Updating book {BookId} rejected: another book {Title} by {Author} already exists",
                    command.BookId, title, author);
                return Result.Failure("Книга з такою назвою та автором уже є в каталозі.");
            }

            var updated = await _bookRepository.UpdateAsync(
                new Book
                {
                    Id = command.BookId,
                    Title = title,
                    Author = author,
                    Genre = command.Genre.Trim(),
                    Year = command.Year,
                    Description = command.Description.Trim()
                },
                cancellationToken);

            if (!updated)
            {
                _logger.LogWarning("Updating book rejected: book {BookId} not found", command.BookId);
                return Result.Failure("Книгу не знайдено.");
            }

            return Result.Success();
        }

        // Use case 4: додати фізичні примірники книги (повертає створені примірники з інвентарними номерами)
        public async Task<Result<IReadOnlyList<BookCopy>>> AddCopiesAsync(AddBookCopiesCommand command, CancellationToken cancellationToken = default)
        {
            var validationError = AddBookCopiesValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Adding book copies rejected: {ValidationError}", validationError);
                return Result.Failure<IReadOnlyList<BookCopy>>(validationError);
            }

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

            return Result.Success(copies);
        }

        // Use case 5: переглянути кількість доступних та виданих книг (усі філії або лише одна)
        public async Task<Result<BookStockDto>> GetStockAsync(GetBookStockQuery query, CancellationToken cancellationToken = default)
        {
            var validationError = GetBookStockValidator.Validate(query);
            if (validationError is not null)
            {
                _logger.LogWarning(
                    "Book stock view rejected: {ValidationError} (book {BookId}, library {LibraryId})",
                    validationError, query.BookId, query.LibraryId);
                return Result.Failure<BookStockDto>(validationError);
            }

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

            return Result.Success(CreateStock(book, counts));
        }

        // Use case 6: списати примірник (виданий або вже списаний примірник списати не можна)
        public async Task<Result> WriteOffAsync(WriteOffBookCopyCommand command, CancellationToken cancellationToken = default)
        {
            var validationError = WriteOffBookCopyValidator.Validate(command);
            if (validationError is not null)
            {
                _logger.LogWarning("Write-off of book copy {BookCopyId} rejected: {ValidationError}", command.BookCopyId, validationError);
                return Result.Failure(validationError);
            }

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
                    command.BookCopyId, bookCopy.Status);
                return Result.Failure(businessRuleError);
            }

            if (!await _bookCopyRepository.SetStatusAsync(command.BookCopyId, BookCopyStatus.WrittenOff, cancellationToken))
            {
                _logger.LogWarning("Write-off rejected: book copy {BookCopyId} disappeared before the status change", command.BookCopyId);
                return Result.Failure("Примірник не знайдено.");
            }

            return Result.Success();
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
