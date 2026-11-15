using BiblioGo.Application.Common;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Lookups.Dtos;
using Microsoft.Extensions.Logging;

namespace BiblioGo.Application.Lookups.Services
{
    // Довідкові списки для випадайок UI (книги, філії, вільні примірники книги).
    // Помилку повертає як неуспішний Result, тож вікна не ловлять винятки
    public class LookupService
    {
        private readonly IBookRepository _books;
        private readonly ILibraryRepository _libraries;
        private readonly IBookCopyRepository _bookCopies;
        private readonly ILogger<LookupService> _logger;

        public LookupService(
            IBookRepository books,
            ILibraryRepository libraries,
            IBookCopyRepository bookCopies,
            ILogger<LookupService> logger)
        {
            _books = books;
            _libraries = libraries;
            _bookCopies = bookCopies;
            _logger = logger;
        }

        // Усі книги: «Назва — Автор»
        public async Task<Result<IReadOnlyList<LookupItemDto>>> GetBooksAsync(CancellationToken cancellationToken = default)
        {
            var books = await _books.GetAllAsync(cancellationToken);

            IReadOnlyList<LookupItemDto> items = books
                .Select(book => new LookupItemDto { Id = book.Id, Name = $"{book.Title} — {book.Author}" })
                .ToList();

            _logger.LogInformation("Loaded {Count} book(s) for dropdown", items.Count);
            return Result.Success(items);
        }

        // Усі філії: назва філії
        public async Task<Result<IReadOnlyList<LookupItemDto>>> GetLibrariesAsync(CancellationToken cancellationToken = default)
        {
            var libraries = await _libraries.GetAllAsync(cancellationToken);

            IReadOnlyList<LookupItemDto> items = libraries
                .Select(library => new LookupItemDto { Id = library.Id, Name = library.Name })
                .ToList();

            _logger.LogInformation("Loaded {Count} librar(ies) for dropdown", items.Count);
            return Result.Success(items);
        }

        // Вільні (Available) примірники книги: «№ інвентарний — філія». Саме їх можна списати
        public async Task<Result<IReadOnlyList<LookupItemDto>>> GetAvailableCopiesAsync(int bookId, CancellationToken cancellationToken = default)
        {
            if (bookId <= 0)
            {
                _logger.LogWarning("Loading copies rejected: invalid book id {BookId}", bookId);
                return Result.Failure<IReadOnlyList<LookupItemDto>>("Оберіть книгу.");
            }

            var copies = await _bookCopies.GetAvailableByBookAsync(bookId, cancellationToken);

            IReadOnlyList<LookupItemDto> items = copies
                .Select(copy => new LookupItemDto { Id = copy.Id, Name = $"№ {copy.InventoryNumber} — {copy.Library.Name}" })
                .ToList();

            _logger.LogInformation("Loaded {Count} available copie(s) of book {BookId} for dropdown", items.Count, bookId);
            return Result.Success(items);
        }
    }
}
