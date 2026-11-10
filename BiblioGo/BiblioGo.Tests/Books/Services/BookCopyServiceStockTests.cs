using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути кількість доступних та виданих книг» (BookCopyService.GetStockAsync)
    public class BookCopyServiceStockTests
    {
        private const int BookId = 7;
        private const int LibraryId = 3;

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceStockTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task GetStockAsync_ExistingBook_ReturnsSuccessWithBookInfo()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCounts(available: 1, issued: 0, writtenOff: 0);

            // Act
            var result = await _service.GetStockAsync(CreateQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(BookId, result.Value.BookId);
            Assert.Equal("Кобзар", result.Value.Title);
            Assert.Equal("Тарас Шевченко", result.Value.Author);
        }

        [Fact]
        public async Task GetStockAsync_CopiesInAllStatuses_ReturnsCountsPerStatus()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCounts(available: 3, issued: 2, writtenOff: 1);

            // Act
            var result = await _service.GetStockAsync(CreateQuery());

            // Assert
            Assert.Equal(3, result.Value.AvailableCopies);
            Assert.Equal(2, result.Value.IssuedCopies);
            Assert.Equal(1, result.Value.WrittenOffCopies);
        }

        [Fact]
        public async Task GetStockAsync_CopiesInAllStatuses_TotalExcludesWrittenOffCopies()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCounts(available: 3, issued: 2, writtenOff: 4);

            // Act
            var result = await _service.GetStockAsync(CreateQuery());

            // Assert
            Assert.Equal(5, result.Value.TotalCopies);
        }

        [Fact]
        public async Task GetStockAsync_BookWithoutCopies_ReturnsSuccessWithZeroCounts()
        {
            // Arrange
            SetupBook(CreateBook());
            IReadOnlyDictionary<BookCopyStatus, int> noCopies = new Dictionary<BookCopyStatus, int>();
            _bookCopyRepository
                .Setup(r => r.CountByStatusAsync(BookId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(noCopies);

            // Act
            var result = await _service.GetStockAsync(CreateQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.TotalCopies);
            Assert.Equal(0, result.Value.AvailableCopies);
            Assert.Equal(0, result.Value.IssuedCopies);
            Assert.Equal(0, result.Value.WrittenOffCopies);
        }

        [Fact]
        public async Task GetStockAsync_NoLibrarySelected_CountsAcrossAllLibraries()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCounts(available: 1, issued: 1, writtenOff: 0);

            // Act
            await _service.GetStockAsync(CreateQuery());

            // Assert
            _bookCopyRepository.Verify(
                r => r.CountByStatusAsync(BookId, null, It.IsAny<CancellationToken>()),
                Times.Once);
            _libraryRepository.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetStockAsync_LibrarySelected_CountsOnlyInThatLibrary()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupLibraryExists(true);
            SetupCounts(available: 1, issued: 1, writtenOff: 0);

            // Act
            var result = await _service.GetStockAsync(CreateQuery(LibraryId));

            // Assert
            Assert.True(result.IsSuccess);
            _bookCopyRepository.Verify(
                r => r.CountByStatusAsync(BookId, LibraryId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetStockAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.GetStockAsync(new GetBookStockQuery { BookId = bookId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetStockAsync_InvalidLibraryId_ReturnsFailureWithSelectLibraryMessage(int libraryId)
        {
            // Act
            var result = await _service.GetStockAsync(CreateQuery(libraryId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть філію бібліотеки", result.Error);
        }

        [Fact]
        public async Task GetStockAsync_InvalidQuery_DoesNotQueryRepositories()
        {
            // Act
            await _service.GetStockAsync(new GetBookStockQuery { BookId = 0 });

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _bookCopyRepository.Verify(
                r => r.CountByStatusAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetStockAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(null);

            // Act
            var result = await _service.GetStockAsync(CreateQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            _bookCopyRepository.Verify(
                r => r.CountByStatusAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetStockAsync_LibraryDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupLibraryExists(false);

            // Act
            var result = await _service.GetStockAsync(CreateQuery(LibraryId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Філію не знайдено.", result.Error);
            _bookCopyRepository.Verify(
                r => r.CountByStatusAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetStockAsync_CancellationToken_PassedToRepositories()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            IReadOnlyDictionary<BookCopyStatus, int> counts = new Dictionary<BookCopyStatus, int>();
            _bookRepository.Setup(r => r.GetByIdAsync(BookId, token)).ReturnsAsync(CreateBook());
            _libraryRepository.Setup(r => r.ExistsAsync(LibraryId, token)).ReturnsAsync(true);
            _bookCopyRepository.Setup(r => r.CountByStatusAsync(BookId, LibraryId, token)).ReturnsAsync(counts);

            // Act
            var result = await _service.GetStockAsync(CreateQuery(LibraryId), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.GetByIdAsync(BookId, token), Times.Once);
            _libraryRepository.Verify(r => r.ExistsAsync(LibraryId, token), Times.Once);
            _bookCopyRepository.Verify(r => r.CountByStatusAsync(BookId, LibraryId, token), Times.Once);
        }

        private void SetupBook(Book? book)
        {
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(book);
        }

        private void SetupLibraryExists(bool exists)
        {
            _libraryRepository
                .Setup(r => r.ExistsAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);
        }

        // Статус із нульовою кількістю репозиторій у словник не додає (як і справжній GROUP BY)
        private void SetupCounts(int available, int issued, int writtenOff)
        {
            var counts = new Dictionary<BookCopyStatus, int>();

            if (available > 0)
            {
                counts[BookCopyStatus.Available] = available;
            }

            if (issued > 0)
            {
                counts[BookCopyStatus.Issued] = issued;
            }

            if (writtenOff > 0)
            {
                counts[BookCopyStatus.WrittenOff] = writtenOff;
            }

            IReadOnlyDictionary<BookCopyStatus, int> result = counts;
            _bookCopyRepository
                .Setup(r => r.CountByStatusAsync(BookId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        private static GetBookStockQuery CreateQuery(int? libraryId = null)
        {
            return new GetBookStockQuery { BookId = BookId, LibraryId = libraryId };
        }

        private static Book CreateBook()
        {
            return new Book { Id = BookId, Title = "Кобзар", Author = "Тарас Шевченко", Genre = "Поезія", Year = 1840 };
        }
    }
}
