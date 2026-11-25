using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути схожі книги» (BookCatalogService.GetSimilarBooksAsync)
    public class BookCatalogServiceSimilarBooksTests
    {
        private const int SelectedBookId = 7;

        private static readonly Book SelectedBook = new()
        {
            Id = SelectedBookId,
            Title = "Кобзар",
            Author = "Тарас Шевченко",
            Genre = "Поезія",
            Year = 1840
        };

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly BookCatalogService _service;

        public BookCatalogServiceSimilarBooksTests()
        {
            _service = new BookCatalogService(
                _bookRepository.Object,
                Mock.Of<IBookCopyRepository>(),
                Mock.Of<ILibraryRepository>(),
                NullLogger<BookCatalogService>.Instance);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_SimilarBooksExist_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            SetupSelectedBook();
            IReadOnlyList<SimilarBookDto> books =
            [
                CreateSimilarBook(1, "Гайдамаки", sameAuthor: true, sameGenre: true),
                CreateSimilarBook(2, "Лісова пісня", sameAuthor: false, sameGenre: true)
            ];
            SetupSimilarBooks(books);

            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(books, result.Value);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_NoSimilarBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupSelectedBook();
            SetupSimilarBooks([]);

            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_BookExists_SearchesByAuthorAndGenreOfSelectedBook()
        {
            // Arrange
            SetupSelectedBook();
            SetupSimilarBooks([]);

            // Act
            await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId, Top = 5 });

            // Assert
            _bookRepository.Verify(
                r => r.GetSimilarAsync(SelectedBookId, "Тарас Шевченко", "Поезія", 5, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_BookNotFound_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetByIdAsync(SelectedBookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Book?)null);

            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
            VerifySimilarBooksNotRequested();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetSimilarBooksAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = bookId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу.", result.Error);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(BookCatalogService.MaxSimilarBooksTop)]
        public async Task GetSimilarBooksAsync_TopOnBoundary_PassesTopToRepository(int top)
        {
            // Arrange
            SetupSelectedBook();
            SetupSimilarBooks([]);

            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId, Top = top });

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(
                r => r.GetSimilarAsync(SelectedBookId, It.IsAny<string>(), It.IsAny<string>(), top, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(BookCatalogService.MaxSimilarBooksTop + 1)]
        public async Task GetSimilarBooksAsync_TopOutOfRange_ReturnsFailureWithTopMessage(int top)
        {
            // Act
            var result = await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId, Top = top });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Кількість схожих книг має бути від 1 до {BookCatalogService.MaxSimilarBooksTop}.", result.Error);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_InvalidQuery_DoesNotQueryRepository()
        {
            // Act
            await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = 0 });

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifySimilarBooksNotRequested();
        }

        [Fact]
        public async Task GetSimilarBooksAsync_LoadingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            SetupSelectedBook();
            var canceledException = new OperationCanceledException();
            _bookRepository
                .Setup(r => r.GetSimilarAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(
                () => _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId }));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task GetSimilarBooksAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            SetupSelectedBook();
            SetupSimilarBooks([]);

            // Act
            await _service.GetSimilarBooksAsync(new GetSimilarBooksQuery { BookId = SelectedBookId }, cancellationTokenSource.Token);

            // Assert
            _bookRepository.Verify(r => r.GetByIdAsync(SelectedBookId, cancellationTokenSource.Token), Times.Once);
            _bookRepository.Verify(
                r => r.GetSimilarAsync(SelectedBookId, "Тарас Шевченко", "Поезія", 10, cancellationTokenSource.Token),
                Times.Once);
        }

        [Theory]
        [InlineData(true, true, "Той самий автор і жанр")]
        [InlineData(true, false, "Той самий автор")]
        [InlineData(false, true, "Той самий жанр")]
        public void SimilarBookDto_Reason_ExplainsWhyBookIsSimilar(bool sameAuthor, bool sameGenre, string expectedReason)
        {
            // Arrange
            var book = CreateSimilarBook(1, "Гайдамаки", sameAuthor, sameGenre);

            // Act
            var reason = book.Reason;

            // Assert
            Assert.Equal(expectedReason, reason);
        }

        private void SetupSelectedBook()
        {
            _bookRepository
                .Setup(r => r.GetByIdAsync(SelectedBookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SelectedBook);
        }

        private void SetupSimilarBooks(IReadOnlyList<SimilarBookDto> books)
        {
            _bookRepository
                .Setup(r => r.GetSimilarAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);
        }

        private void VerifySimilarBooksNotRequested()
        {
            _bookRepository.Verify(
                r => r.GetSimilarAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static SimilarBookDto CreateSimilarBook(int id, string title, bool sameAuthor, bool sameGenre)
        {
            return new SimilarBookDto
            {
                BookId = id,
                Title = title,
                Author = "Тарас Шевченко",
                Genre = "Поезія",
                Year = 1845,
                SameAuthor = sameAuthor,
                SameGenre = sameGenre
            };
        }
    }
}
