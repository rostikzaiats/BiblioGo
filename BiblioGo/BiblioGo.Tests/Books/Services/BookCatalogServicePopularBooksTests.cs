using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути популярні книги» (BookCatalogService.GetPopularBooksAsync)
    public class BookCatalogServicePopularBooksTests
    {
        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly BookCatalogService _service;

        public BookCatalogServicePopularBooksTests()
        {
            _service = new BookCatalogService(
                _bookRepository.Object,
                Mock.Of<IBookCopyRepository>(),
                NullLogger<BookCatalogService>.Instance);
        }

        [Fact]
        public async Task GetPopularBooksAsync_BooksExist_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<PopularBookDto> books = [CreateBook(1, "Кобзар", 5), CreateBook(2, "Лісова пісня", 3)];
            SetupPopularBooks(books);

            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(books, result.Value);
            Assert.Equal(5, result.Value[0].IssueCount);
        }

        [Fact]
        public async Task GetPopularBooksAsync_BooksExist_NumbersBooksInRepositoryOrder()
        {
            // Arrange
            SetupPopularBooks([CreateBook(1, "Кобзар", 5), CreateBook(2, "Лісова пісня", 3), CreateBook(3, "Тіні", 3)]);

            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery());

            // Assert
            Assert.Equal([1, 2, 3], result.Value.Select(book => book.Rank));
        }

        [Fact]
        public async Task GetPopularBooksAsync_NoIssuedBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupPopularBooks([]);

            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetPopularBooksAsync_AllTime_DoesNotLimitIssueDate()
        {
            // Arrange
            SetupPopularBooks([]);

            // Act
            await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Period = PopularBooksPeriod.AllTime });

            // Assert
            _bookRepository.Verify(r => r.GetPopularAsync(null, 10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(PopularBooksPeriod.LastMonth, 1)]
        [InlineData(PopularBooksPeriod.LastThreeMonths, 3)]
        [InlineData(PopularBooksPeriod.LastYear, 12)]
        public async Task GetPopularBooksAsync_LimitedPeriod_CountsFromPeriodStartInUtc(PopularBooksPeriod period, int months)
        {
            // Arrange
            DateTime? issuedFrom = null;
            _bookRepository
                .Setup(r => r.GetPopularAsync(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<DateTime?, int, CancellationToken>((from, _, _) => issuedFrom = from)
                .ReturnsAsync([]);
            var earliest = DateTime.UtcNow.AddMonths(-months);

            // Act
            await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Period = period });
            var latest = DateTime.UtcNow.AddMonths(-months);

            // Assert
            Assert.NotNull(issuedFrom);
            Assert.Equal(DateTimeKind.Utc, issuedFrom.Value.Kind);
            Assert.InRange(issuedFrom.Value, earliest, latest);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(BookCatalogService.MaxPopularBooksTop)]
        public async Task GetPopularBooksAsync_TopOnBoundary_PassesTopToRepository(int top)
        {
            // Arrange
            SetupPopularBooks([]);

            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Top = top });

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.GetPopularAsync(null, top, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(BookCatalogService.MaxPopularBooksTop + 1)]
        public async Task GetPopularBooksAsync_TopOutOfRange_ReturnsFailureWithTopMessage(int top)
        {
            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Top = top });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Кількість книг у рейтингу має бути від 1 до {BookCatalogService.MaxPopularBooksTop}.", result.Error);
        }

        [Fact]
        public async Task GetPopularBooksAsync_UnknownPeriod_ReturnsFailureWithPeriodMessage()
        {
            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Period = (PopularBooksPeriod)99 });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть період.", result.Error);
        }

        [Fact]
        public async Task GetPopularBooksAsync_InvalidQuery_DoesNotQueryRepository()
        {
            // Act
            await _service.GetPopularBooksAsync(new GetPopularBooksQuery { Top = 0 });

            // Assert
            _bookRepository.Verify(
                r => r.GetPopularAsync(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPopularBooksAsync_RepositoryFails_ReturnsFailureWithLoadFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetPopularAsync(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetPopularBooksAsync(new GetPopularBooksQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Не вдалося завантажити популярні книги. Спробуйте пізніше.", result.Error);
        }

        [Fact]
        public async Task GetPopularBooksAsync_LoadingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            _bookRepository
                .Setup(r => r.GetPopularAsync(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.GetPopularBooksAsync(new GetPopularBooksQuery()));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task GetPopularBooksAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            SetupPopularBooks([]);

            // Act
            await _service.GetPopularBooksAsync(new GetPopularBooksQuery(), cancellationTokenSource.Token);

            // Assert
            _bookRepository.Verify(r => r.GetPopularAsync(null, 10, cancellationTokenSource.Token), Times.Once);
        }

        [Fact]
        public async Task GetPopularBooksAsync_NullQuery_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _service.GetPopularBooksAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        private void SetupPopularBooks(IReadOnlyList<PopularBookDto> books)
        {
            _bookRepository
                .Setup(r => r.GetPopularAsync(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);
        }

        private static PopularBookDto CreateBook(int id, string title, int issueCount)
        {
            return new PopularBookDto { BookId = id, Title = title, Author = "Автор", Genre = "Поезія", Year = 1840, IssueCount = issueCount };
        }
    }
}
