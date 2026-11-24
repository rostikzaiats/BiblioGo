using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути примірники книги» (BookCopyService.GetCopiesAsync)
    public class BookCopyServiceCopiesTests
    {
        private const int BookId = 7;
        private const int LibraryId = 3;

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceCopiesTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task GetCopiesAsync_BookWithCopies_ReturnsSuccessWithCopyDetails()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCopies(
                CreateCopy(1, 1001, BookCopyStatus.Available, "Центральна"),
                CreateCopy(2, 1002, BookCopyStatus.Issued, "Філія №2"));

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Error);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(1, result.Value[0].BookCopyId);
            Assert.Equal(1001, result.Value[0].InventoryNumber);
            Assert.Equal("Центральна", result.Value[0].LibraryName);
            Assert.Equal(BookCopyStatus.Available, result.Value[0].Status);
            Assert.Equal(BookCopyStatus.Issued, result.Value[1].Status);
        }

        [Fact]
        public async Task GetCopiesAsync_WrittenOffCopy_IsIncludedInResult()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCopies(CreateCopy(5, 1005, BookCopyStatus.WrittenOff, "Центральна"));

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(BookCopyStatus.WrittenOff, result.Value[0].Status);
        }

        [Fact]
        public async Task GetCopiesAsync_BookWithoutCopies_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCopies();

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetCopiesAsync_NoFilters_LoadsCopiesFromAllLibrariesInAllStatuses()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupCopies();

            // Act
            await _service.GetCopiesAsync(CreateQuery());

            // Assert
            _bookCopyRepository.Verify(
                r => r.GetByBookAsync(BookId, null, null, It.IsAny<CancellationToken>()),
                Times.Once);
            _libraryRepository.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCopiesAsync_LibraryAndStatusSelected_PassesFiltersToRepository()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupLibraryExists(true);
            SetupCopies();

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery(LibraryId, BookCopyStatus.Issued));

            // Assert
            Assert.True(result.IsSuccess);
            _bookCopyRepository.Verify(
                r => r.GetByBookAsync(BookId, LibraryId, BookCopyStatus.Issued, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetCopiesAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.GetCopiesAsync(new GetBookCopiesQuery { BookId = bookId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetCopiesAsync_InvalidLibraryId_ReturnsFailureWithSelectLibraryMessage(int libraryId)
        {
            // Act
            var result = await _service.GetCopiesAsync(CreateQuery(libraryId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть філію бібліотеки", result.Error);
        }

        [Fact]
        public async Task GetCopiesAsync_UnknownStatus_ReturnsFailureWithSelectStatusMessage()
        {
            // Act
            var result = await _service.GetCopiesAsync(CreateQuery(status: (BookCopyStatus)99));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть статус примірника", result.Error);
        }

        [Fact]
        public async Task GetCopiesAsync_InvalidQuery_DoesNotQueryRepositories()
        {
            // Act
            await _service.GetCopiesAsync(new GetBookCopiesQuery { BookId = 0 });

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyCopiesNeverLoaded();
        }

        [Fact]
        public async Task GetCopiesAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(null);

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            VerifyCopiesNeverLoaded();
        }

        [Fact]
        public async Task GetCopiesAsync_LibraryDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupLibraryExists(false);

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery(LibraryId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Філію не знайдено.", result.Error);
            VerifyCopiesNeverLoaded();
        }

        [Fact]
        public async Task GetCopiesAsync_CancellationToken_PassedToRepositories()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            IReadOnlyList<BookCopy> noCopies = new List<BookCopy>();
            _bookRepository.Setup(r => r.GetByIdAsync(BookId, token)).ReturnsAsync(CreateBook());
            _libraryRepository.Setup(r => r.ExistsAsync(LibraryId, token)).ReturnsAsync(true);
            _bookCopyRepository.Setup(r => r.GetByBookAsync(BookId, LibraryId, null, token)).ReturnsAsync(noCopies);

            // Act
            var result = await _service.GetCopiesAsync(CreateQuery(LibraryId), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.GetByIdAsync(BookId, token), Times.Once);
            _libraryRepository.Verify(r => r.ExistsAsync(LibraryId, token), Times.Once);
            _bookCopyRepository.Verify(r => r.GetByBookAsync(BookId, LibraryId, null, token), Times.Once);
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

        private void SetupCopies(params BookCopy[] copies)
        {
            IReadOnlyList<BookCopy> result = copies;
            _bookCopyRepository
                .Setup(r => r.GetByBookAsync(BookId, It.IsAny<int?>(), It.IsAny<BookCopyStatus?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        private void VerifyCopiesNeverLoaded()
        {
            _bookCopyRepository.Verify(
                r => r.GetByBookAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<BookCopyStatus?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static GetBookCopiesQuery CreateQuery(int? libraryId = null, BookCopyStatus? status = null)
        {
            return new GetBookCopiesQuery { BookId = BookId, LibraryId = libraryId, Status = status };
        }

        private static Book CreateBook()
        {
            return new Book { Id = BookId, Title = "Кобзар", Author = "Тарас Шевченко", Genre = "Поезія", Year = 1840 };
        }

        private static BookCopy CreateCopy(int id, int inventoryNumber, BookCopyStatus status, string libraryName)
        {
            return new BookCopy
            {
                Id = id,
                BookId = BookId,
                InventoryNumber = inventoryNumber,
                Status = status,
                LibraryId = LibraryId,
                Library = new Library { Id = LibraryId, Name = libraryName }
            };
        }
    }
}
