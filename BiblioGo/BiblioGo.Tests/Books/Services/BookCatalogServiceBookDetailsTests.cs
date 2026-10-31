using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути інформацію про книгу» (BookCatalogService.GetBookDetailsAsync)
    public class BookCatalogServiceBookDetailsTests
    {
        private const int BookId = 7;
        private const string LoadFailedMessage = "Не вдалося завантажити інформацію про книгу. Спробуйте пізніше.";

        private static readonly Library CentralLibrary = new() { Id = 1, Name = "Центральна", Address = "вул. Шевченка, 1" };
        private static readonly Library RiversideLibrary = new() { Id = 2, Name = "Подільська", Address = "вул. Набережна, 5" };

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly BookCatalogService _service;

        public BookCatalogServiceBookDetailsTests()
        {
            _service = new BookCatalogService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                NullLogger<BookCatalogService>.Instance);
        }

        [Fact]
        public async Task GetBookDetailsAsync_ExistingBook_ReturnsSuccessWithBookInfo()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupAvailability([], issuedCopies: 0);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(BookId, result.Value.Id);
            Assert.Equal("Кобзар", result.Value.Title);
            Assert.Equal("Тарас Шевченко", result.Value.Author);
            Assert.Equal("Поезія", result.Value.Genre);
            Assert.Equal(1840, result.Value.Year);
            Assert.Equal("Збірка поетичних творів.", result.Value.Description);
        }

        [Fact]
        public async Task GetBookDetailsAsync_LoanableAndIssuedCopies_ReturnsBothCounts()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupAvailability(
                [CreateCopy(1, CentralLibrary), CreateCopy(2, RiversideLibrary)],
                issuedCopies: 1);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.Equal(2, result.Value.AvailableCopies);
            Assert.Equal(1, result.Value.IssuedCopies);
        }

        [Fact]
        public async Task GetBookDetailsAsync_AvailableCopiesInSeveralLibraries_GroupsByLibraryMostCopiesFirst()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupAvailability(
                [CreateCopy(1, CentralLibrary), CreateCopy(2, RiversideLibrary), CreateCopy(3, RiversideLibrary)],
                issuedCopies: 1);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            var libraries = result.Value.AvailableInLibraries;
            Assert.Equal(2, libraries.Count);
            Assert.Equal("Подільська", libraries[0].LibraryName);
            Assert.Equal("вул. Набережна, 5", libraries[0].Address);
            Assert.Equal(2, libraries[0].AvailableCopies);
            Assert.Equal("Центральна", libraries[1].LibraryName);
            Assert.Equal(1, libraries[1].AvailableCopies);
        }

        [Fact]
        public async Task GetBookDetailsAsync_OnlyIssuedCopies_ReturnsNoAvailableLibraries()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupAvailability([], issuedCopies: 1);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.AvailableCopies);
            Assert.Equal(1, result.Value.IssuedCopies);
            Assert.Empty(result.Value.AvailableInLibraries);
        }

        [Fact]
        public async Task GetBookDetailsAsync_NoCopies_ReturnsSuccessWithZeroCounts()
        {
            // Arrange
            SetupBook(CreateBook());
            SetupAvailability([], issuedCopies: 0);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.AvailableCopies);
            Assert.Equal(0, result.Value.IssuedCopies);
            Assert.Empty(result.Value.AvailableInLibraries);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetBookDetailsAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.GetBookDetailsAsync(bookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу.", result.Error);
        }

        [Fact]
        public async Task GetBookDetailsAsync_BookNotSelected_DoesNotQueryRepositories()
        {
            // Act
            await _service.GetBookDetailsAsync(0);

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _bookCopyRepository.Verify(
                r => r.GetLoanableByBookAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _bookCopyRepository.Verify(
                r => r.CountIssuedByBookAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetBookDetailsAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(null);

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            _bookCopyRepository.Verify(
                r => r.GetLoanableByBookAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetBookDetailsAsync_BookRepositoryFails_ReturnsFailureWithLoadFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(LoadFailedMessage, result.Error);
        }

        [Fact]
        public async Task GetBookDetailsAsync_CopyRepositoryFails_ReturnsFailureWithLoadFailedMessage()
        {
            // Arrange
            SetupBook(CreateBook());
            _bookCopyRepository
                .Setup(r => r.GetLoanableByBookAsync(BookId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(LoadFailedMessage, result.Error);
        }

        [Fact]
        public async Task GetBookDetailsAsync_IssuedCountFails_ReturnsFailureWithLoadFailedMessage()
        {
            // Arrange
            SetupBook(CreateBook());
            _bookCopyRepository
                .Setup(r => r.GetLoanableByBookAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _bookCopyRepository
                .Setup(r => r.CountIssuedByBookAsync(BookId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetBookDetailsAsync(BookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(LoadFailedMessage, result.Error);
        }

        [Fact]
        public async Task GetBookDetailsAsync_LoadingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.GetBookDetailsAsync(BookId));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task GetBookDetailsAsync_CancellationToken_PassedToRepositories()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            SetupBook(CreateBook());
            SetupAvailability([], issuedCopies: 0);

            // Act
            await _service.GetBookDetailsAsync(BookId, cancellationTokenSource.Token);

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(BookId, cancellationTokenSource.Token), Times.Once);
            _bookCopyRepository.Verify(r => r.GetLoanableByBookAsync(BookId, cancellationTokenSource.Token), Times.Once);
            _bookCopyRepository.Verify(r => r.CountIssuedByBookAsync(BookId, cancellationTokenSource.Token), Times.Once);
        }

        private void SetupBook(Book? book)
        {
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(book);
        }

        private void SetupAvailability(IReadOnlyList<BookCopy> loanableCopies, int issuedCopies)
        {
            _bookCopyRepository
                .Setup(r => r.GetLoanableByBookAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(loanableCopies);
            _bookCopyRepository
                .Setup(r => r.CountIssuedByBookAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(issuedCopies);
        }

        private static Book CreateBook()
        {
            return new Book
            {
                Id = BookId,
                Title = "Кобзар",
                Author = "Тарас Шевченко",
                Genre = "Поезія",
                Year = 1840,
                Description = "Збірка поетичних творів."
            };
        }

        private static BookCopy CreateCopy(int id, Library library)
        {
            return new BookCopy
            {
                Id = id,
                BookId = BookId,
                InventoryNumber = 1000 + id,
                LibraryId = library.Id,
                Library = library
            };
        }
    }
}
