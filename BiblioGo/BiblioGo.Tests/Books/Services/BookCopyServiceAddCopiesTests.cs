using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Додати фізичні примірники книги» (BookCopyService.AddCopiesAsync)
    public class BookCopyServiceAddCopiesTests
    {
        private const int BookId = 2;
        private const int LibraryId = 3;
        private const int Quantity = 3;
        private const string AddFailedMessage = "Не вдалося додати примірники. Спробуйте пізніше.";
        private const string QuantityMessage = "Кількість примірників має бути від 1 до 100";

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceAddCopiesTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task AddCopiesAsync_ValidCommand_ReturnsSuccessWithCreatedCopies()
        {
            // Arrange
            var createdCopies = CreateCopies(Quantity);
            SetupBookAndLibraryExist();
            SetupAddCopies(createdCopies);

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(createdCopies, result.Value);
        }

        [Fact]
        public async Task AddCopiesAsync_ValidCommand_PassesCommandDataToRepository()
        {
            // Arrange
            SetupBookAndLibraryExist();
            SetupAddCopies(CreateCopies(Quantity));

            // Act
            await _service.AddCopiesAsync(CreateCommand());

            // Assert
            _bookCopyRepository.Verify(
                r => r.AddCopiesAsync(BookId, LibraryId, Quantity, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task AddCopiesAsync_QuantityOnBoundary_ReturnsSuccess(int quantity)
        {
            // Arrange
            SetupBookAndLibraryExist();
            SetupAddCopies(CreateCopies(quantity));

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand(quantity: quantity));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(quantity, result.Value.Count);
        }

        [Theory]
        [InlineData(0, LibraryId, Quantity, "Оберіть книгу")]
        [InlineData(-1, LibraryId, Quantity, "Оберіть книгу")]
        [InlineData(BookId, 0, Quantity, "Оберіть філію бібліотеки")]
        [InlineData(BookId, -1, Quantity, "Оберіть філію бібліотеки")]
        [InlineData(BookId, LibraryId, 0, QuantityMessage)]
        [InlineData(BookId, LibraryId, -1, QuantityMessage)]
        [InlineData(BookId, LibraryId, 101, QuantityMessage)]
        public async Task AddCopiesAsync_InvalidCommand_ReturnsFailureWithValidationMessage(
            int bookId, int libraryId, int quantity, string expectedError)
        {
            // Act
            var result = await _service.AddCopiesAsync(CreateCommand(bookId, libraryId, quantity));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
        }

        [Fact]
        public async Task AddCopiesAsync_InvalidCommand_DoesNotQueryRepositories()
        {
            // Act
            await _service.AddCopiesAsync(CreateCommand(quantity: 0));

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _libraryRepository.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyCopiesNeverAdded();
        }

        [Fact]
        public async Task AddCopiesAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _service.AddCopiesAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task AddCopiesAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupLibraryExists(true);

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            VerifyCopiesNeverAdded();
        }

        [Fact]
        public async Task AddCopiesAsync_LibraryDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBookExists(true);
            SetupLibraryExists(false);

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Філію не знайдено.", result.Error);
            VerifyCopiesNeverAdded();
        }

        [Fact]
        public async Task AddCopiesAsync_BookRepositoryFails_ReturnsFailureWithAddFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AddFailedMessage, result.Error);
        }

        [Fact]
        public async Task AddCopiesAsync_LibraryRepositoryFails_ReturnsFailureWithAddFailedMessage()
        {
            // Arrange
            SetupBookExists(true);
            _libraryRepository
                .Setup(r => r.ExistsAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AddFailedMessage, result.Error);
        }

        [Fact]
        public async Task AddCopiesAsync_AddingCopiesFails_ReturnsFailureWithAddFailedMessage()
        {
            // Arrange
            SetupBookAndLibraryExist();
            _bookCopyRepository
                .Setup(r => r.AddCopiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AddFailedMessage, result.Error);
        }

        [Fact]
        public async Task AddCopiesAsync_AddingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            SetupBookAndLibraryExist();
            _bookCopyRepository
                .Setup(r => r.AddCopiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.AddCopiesAsync(CreateCommand()));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task AddCopiesAsync_CancellationToken_PassedToRepositories()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            _bookRepository.Setup(r => r.GetByIdAsync(BookId, token)).ReturnsAsync(new Book { Id = BookId });
            _libraryRepository.Setup(r => r.ExistsAsync(LibraryId, token)).ReturnsAsync(true);
            _bookCopyRepository
                .Setup(r => r.AddCopiesAsync(BookId, LibraryId, Quantity, token))
                .ReturnsAsync(CreateCopies(Quantity));

            // Act
            var result = await _service.AddCopiesAsync(CreateCommand(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.GetByIdAsync(BookId, token), Times.Once);
            _libraryRepository.Verify(r => r.ExistsAsync(LibraryId, token), Times.Once);
            _bookCopyRepository.Verify(r => r.AddCopiesAsync(BookId, LibraryId, Quantity, token), Times.Once);
        }

        private void SetupBookAndLibraryExist()
        {
            SetupBookExists(true);
            SetupLibraryExists(true);
        }

        private void SetupBookExists(bool exists)
        {
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists ? new Book { Id = BookId } : null);
        }

        private void SetupLibraryExists(bool exists)
        {
            _libraryRepository
                .Setup(r => r.ExistsAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);
        }

        private void SetupAddCopies(IReadOnlyList<BookCopy> copies)
        {
            _bookCopyRepository
                .Setup(r => r.AddCopiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(copies);
        }

        private void VerifyCopiesNeverAdded()
        {
            _bookCopyRepository.Verify(
                r => r.AddCopiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static AddBookCopiesCommand CreateCommand(
            int bookId = BookId,
            int libraryId = LibraryId,
            int quantity = Quantity)
        {
            return new AddBookCopiesCommand { BookId = bookId, LibraryId = libraryId, Quantity = quantity };
        }

        private static IReadOnlyList<BookCopy> CreateCopies(int quantity)
        {
            return Enumerable.Range(1, quantity)
                .Select(number => new BookCopy
                {
                    Id = number,
                    BookId = BookId,
                    LibraryId = LibraryId,
                    InventoryNumber = 1000 + number,
                    Status = BookCopyStatus.Available
                })
                .ToList();
        }
    }
}
