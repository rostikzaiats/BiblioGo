using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Видалити книгу з каталогу» (BookCopyService.DeleteBookAsync)
    public class BookCopyServiceDeleteBookTests
    {
        private const int BookId = 6;

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceDeleteBookTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task DeleteBookAsync_BookWithoutActiveLoans_ReturnsSuccess()
        {
            // Arrange
            SetupBook(exists: true);
            SetupActiveLoans(hasActiveLoans: false);
            SetupDelete(deleted: true);

            // Act
            var result = await _service.DeleteBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Error);
        }

        [Fact]
        public async Task DeleteBookAsync_BookWithoutActiveLoans_DeletesThisBook()
        {
            // Arrange
            SetupBook(exists: true);
            SetupActiveLoans(hasActiveLoans: false);
            SetupDelete(deleted: true);

            // Act
            await _service.DeleteBookAsync(CreateCommand());

            // Assert
            _bookRepository.Verify(r => r.DeleteAsync(BookId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteBookAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.DeleteBookAsync(CreateCommand(bookId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу", result.Error);
        }

        [Fact]
        public async Task DeleteBookAsync_BookNotSelected_DoesNotQueryRepository()
        {
            // Act
            await _service.DeleteBookAsync(CreateCommand(0));

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _bookRepository.Verify(r => r.HasActiveLoansAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyBookNeverDeleted();
        }

        [Fact]
        public async Task DeleteBookAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(exists: false);

            // Act
            var result = await _service.DeleteBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            VerifyBookNeverDeleted();
        }

        [Fact]
        public async Task DeleteBookAsync_BookHasActiveLoans_ReturnsFailureWithActiveLoansMessage()
        {
            // Arrange
            SetupBook(exists: true);
            SetupActiveLoans(hasActiveLoans: true);

            // Act
            var result = await _service.DeleteBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Не можна видалити книгу: за нею є активні заявки або видані примірники.", result.Error);
        }

        [Fact]
        public async Task DeleteBookAsync_BookHasActiveLoans_DoesNotDeleteBook()
        {
            // Arrange
            SetupBook(exists: true);
            SetupActiveLoans(hasActiveLoans: true);

            // Act
            await _service.DeleteBookAsync(CreateCommand());

            // Assert
            VerifyBookNeverDeleted();
        }

        [Fact]
        public async Task DeleteBookAsync_BookDeletedByAnotherUserMeanwhile_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupBook(exists: true);
            SetupActiveLoans(hasActiveLoans: false);
            SetupDelete(deleted: false);

            // Act
            var result = await _service.DeleteBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
        }

        [Fact]
        public async Task DeleteBookAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            _bookRepository.Setup(r => r.GetByIdAsync(BookId, token)).ReturnsAsync(new Book { Id = BookId });
            _bookRepository.Setup(r => r.HasActiveLoansAsync(BookId, token)).ReturnsAsync(false);
            _bookRepository.Setup(r => r.DeleteAsync(BookId, token)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteBookAsync(CreateCommand(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.GetByIdAsync(BookId, token), Times.Once);
            _bookRepository.Verify(r => r.HasActiveLoansAsync(BookId, token), Times.Once);
            _bookRepository.Verify(r => r.DeleteAsync(BookId, token), Times.Once);
        }

        private void SetupBook(bool exists)
        {
            _bookRepository
                .Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists ? new Book { Id = BookId } : null);
        }

        private void SetupActiveLoans(bool hasActiveLoans)
        {
            _bookRepository
                .Setup(r => r.HasActiveLoansAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(hasActiveLoans);
        }

        private void SetupDelete(bool deleted)
        {
            _bookRepository
                .Setup(r => r.DeleteAsync(BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleted);
        }

        private void VerifyBookNeverDeleted()
        {
            _bookRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static DeleteBookCommand CreateCommand(int bookId = BookId)
        {
            return new DeleteBookCommand { BookId = bookId };
        }
    }
}
