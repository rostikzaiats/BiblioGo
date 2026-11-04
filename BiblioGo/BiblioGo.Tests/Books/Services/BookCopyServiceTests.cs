using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Списати примірник» (BookCopyService.WriteOffAsync)
    public class BookCopyServiceTests
    {
        private const int BookCopyId = 7;
        private const string WriteOffFailedMessage = "Не вдалося списати примірник. Спробуйте пізніше.";

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task WriteOffAsync_AvailableCopy_ReturnsSuccessAndSetsWrittenOffStatus()
        {
            // Arrange
            SetupCopy(CreateCopy(BookCopyStatus.Available));
            SetupStatusChange(updated: true);

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Error);
            _bookCopyRepository.Verify(
                r => r.SetStatusAsync(BookCopyId, BookCopyStatus.WrittenOff, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task WriteOffAsync_IssuedCopy_ReturnsFailureAndDoesNotChangeStatus()
        {
            // Arrange
            SetupCopy(CreateCopy(BookCopyStatus.Issued));

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Не можна списати виданий примірник. Спочатку оформіть його повернення.", result.Error);
            VerifyStatusNeverChanged();
        }

        [Fact]
        public async Task WriteOffAsync_AlreadyWrittenOffCopy_ReturnsFailureAndDoesNotChangeStatus()
        {
            // Arrange
            SetupCopy(CreateCopy(BookCopyStatus.WrittenOff));

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Примірник уже списано.", result.Error);
            VerifyStatusNeverChanged();
        }

        [Fact]
        public async Task WriteOffAsync_CopyDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupCopy(null);

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Примірник не знайдено.", result.Error);
            VerifyStatusNeverChanged();
        }

        [Fact]
        public async Task WriteOffAsync_CopyDeletedBeforeStatusChange_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupCopy(CreateCopy(BookCopyStatus.Available));
            SetupStatusChange(updated: false);

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Примірник не знайдено.", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task WriteOffAsync_CopyNotSelected_ReturnsFailureWithSelectCopyMessage(int bookCopyId)
        {
            // Act
            var result = await _service.WriteOffAsync(CreateCommand(bookCopyId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть примірник", result.Error);
        }

        [Fact]
        public async Task WriteOffAsync_CopyNotSelected_DoesNotQueryRepository()
        {
            // Act
            await _service.WriteOffAsync(CreateCommand(0));

            // Assert
            _bookCopyRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyStatusNeverChanged();
        }

        [Fact]
        public async Task WriteOffAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _service.WriteOffAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task WriteOffAsync_GetCopyFails_ReturnsFailureWithWriteOffFailedMessage()
        {
            // Arrange
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(WriteOffFailedMessage, result.Error);
        }

        [Fact]
        public async Task WriteOffAsync_SetStatusFails_ReturnsFailureWithWriteOffFailedMessage()
        {
            // Arrange
            SetupCopy(CreateCopy(BookCopyStatus.Available));
            _bookCopyRepository
                .Setup(r => r.SetStatusAsync(BookCopyId, BookCopyStatus.WrittenOff, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(WriteOffFailedMessage, result.Error);
        }

        [Fact]
        public async Task WriteOffAsync_WriteOffCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task WriteOffAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, token))
                .ReturnsAsync(CreateCopy(BookCopyStatus.Available));
            _bookCopyRepository
                .Setup(r => r.SetStatusAsync(BookCopyId, BookCopyStatus.WrittenOff, token))
                .ReturnsAsync(true);

            // Act
            var result = await _service.WriteOffAsync(CreateCommand(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookCopyRepository.Verify(r => r.GetByIdAsync(BookCopyId, token), Times.Once);
            _bookCopyRepository.Verify(r => r.SetStatusAsync(BookCopyId, BookCopyStatus.WrittenOff, token), Times.Once);
        }

        private void SetupCopy(BookCopy? copy)
        {
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(copy);
        }

        private void SetupStatusChange(bool updated)
        {
            _bookCopyRepository
                .Setup(r => r.SetStatusAsync(BookCopyId, BookCopyStatus.WrittenOff, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated);
        }

        private void VerifyStatusNeverChanged()
        {
            _bookCopyRepository.Verify(
                r => r.SetStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static WriteOffBookCopyCommand CreateCommand(int bookCopyId = BookCopyId)
        {
            return new WriteOffBookCopyCommand { BookCopyId = bookCopyId };
        }

        private static BookCopy CreateCopy(BookCopyStatus status)
        {
            return new BookCopy { Id = BookCopyId, BookId = 1, InventoryNumber = 1001, LibraryId = 1, Status = status };
        }
    }
}
