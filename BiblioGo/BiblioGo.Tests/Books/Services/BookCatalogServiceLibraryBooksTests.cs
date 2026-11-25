using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути книги, доступні в бібліотеці» (BookCatalogService.GetAvailableBooksInLibraryAsync)
    public class BookCatalogServiceLibraryBooksTests
    {
        private const int LibraryId = 3;

        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCatalogService _service;

        public BookCatalogServiceLibraryBooksTests()
        {
            _service = new BookCatalogService(
                Mock.Of<IBookRepository>(),
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCatalogService>.Instance);
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_BooksAvailable_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            SetupLibraryExists(true);
            IReadOnlyList<LibraryBookDto> books = [CreateBook(1, "Кобзар", 2), CreateBook(2, "Лісова пісня", 1)];
            SetupAvailableBooks(books);

            // Act
            var result = await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = LibraryId });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(books, result.Value);
            Assert.Equal(2, result.Value[0].AvailableCopies);
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_NoAvailableBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupLibraryExists(true);
            SetupAvailableBooks([]);

            // Act
            var result = await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = LibraryId });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_LibraryExists_SearchesInSelectedLibrary()
        {
            // Arrange
            SetupLibraryExists(true);
            SetupAvailableBooks([]);

            // Act
            await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = LibraryId });

            // Assert
            _bookCopyRepository.Verify(
                r => r.GetAvailableBooksInLibraryAsync(LibraryId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetAvailableBooksInLibraryAsync_LibraryNotSelected_ReturnsFailureWithSelectLibraryMessage(int libraryId)
        {
            // Act
            var result = await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = libraryId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть бібліотеку.", result.Error);
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_LibraryNotSelected_DoesNotQueryRepositories()
        {
            // Act
            await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = 0 });

            // Assert
            _libraryRepository.Verify(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyBooksNotRequested();
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_LibraryNotFound_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupLibraryExists(false);

            // Act
            var result = await _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = LibraryId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Бібліотеку не знайдено.", result.Error);
            VerifyBooksNotRequested();
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_LoadingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            SetupLibraryExists(true);
            var canceledException = new OperationCanceledException();
            _bookCopyRepository
                .Setup(r => r.GetAvailableBooksInLibraryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(
                () => _service.GetAvailableBooksInLibraryAsync(new GetLibraryBooksQuery { LibraryId = LibraryId }));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task GetAvailableBooksInLibraryAsync_CancellationToken_PassedToRepositories()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            SetupLibraryExists(true);
            SetupAvailableBooks([]);

            // Act
            await _service.GetAvailableBooksInLibraryAsync(
                new GetLibraryBooksQuery { LibraryId = LibraryId },
                cancellationTokenSource.Token);

            // Assert
            _libraryRepository.Verify(r => r.ExistsAsync(LibraryId, cancellationTokenSource.Token), Times.Once);
            _bookCopyRepository.Verify(
                r => r.GetAvailableBooksInLibraryAsync(LibraryId, cancellationTokenSource.Token),
                Times.Once);
        }

        private void SetupLibraryExists(bool exists)
        {
            _libraryRepository
                .Setup(r => r.ExistsAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);
        }

        private void SetupAvailableBooks(IReadOnlyList<LibraryBookDto> books)
        {
            _bookCopyRepository
                .Setup(r => r.GetAvailableBooksInLibraryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);
        }

        private void VerifyBooksNotRequested()
        {
            _bookCopyRepository.Verify(
                r => r.GetAvailableBooksInLibraryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static LibraryBookDto CreateBook(int id, string title, int availableCopies)
        {
            return new LibraryBookDto
            {
                BookId = id,
                Title = title,
                Author = "Автор",
                Genre = "Поезія",
                Year = 1840,
                AvailableCopies = availableCopies
            };
        }
    }
}
