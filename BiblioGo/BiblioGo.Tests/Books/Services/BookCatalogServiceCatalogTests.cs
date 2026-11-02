using BiblioGo.Application.Books.Queries;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Переглянути каталог книг» (BookCatalogService.GetCatalogAsync, GetGenresAsync)
    public class BookCatalogServiceCatalogTests
    {
        private const string LoadFailedMessage = "Не вдалося завантажити каталог. Спробуйте пізніше.";

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly BookCatalogService _service;

        public BookCatalogServiceCatalogTests()
        {
            _service = new BookCatalogService(
                _bookRepository.Object,
                Mock.Of<IBookCopyRepository>(),
                NullLogger<BookCatalogService>.Instance);
        }

        [Fact]
        public async Task GetCatalogAsync_DefaultQuery_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<Book> expectedBooks = [CreateBook(1, "Кобзар"), CreateBook(2, "Лісова пісня")];
            SetupCatalog(expectedBooks, totalCount: 2);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedBooks, result.Value.Books);
            Assert.Equal(1, result.Value.PageNumber);
            Assert.Equal(10, result.Value.PageSize);
            Assert.Equal(2, result.Value.TotalCount);
        }

        [Theory]
        [InlineData(0, 10, 0)]
        [InlineData(1, 10, 1)]
        [InlineData(10, 10, 1)]
        [InlineData(11, 10, 2)]
        [InlineData(25, 10, 3)]
        public async Task GetCatalogAsync_TotalCount_CalculatesTotalPages(int totalCount, int pageSize, int expectedTotalPages)
        {
            // Arrange
            SetupCatalog([], totalCount);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { PageSize = pageSize });

            // Assert
            Assert.Equal(expectedTotalPages, result.Value.TotalPages);
        }

        [Theory]
        [InlineData(1, 10, 0)]
        [InlineData(2, 10, 10)]
        [InlineData(3, 5, 10)]
        public async Task GetCatalogAsync_PageNumber_SkipsPreviousPages(int pageNumber, int pageSize, int expectedSkip)
        {
            // Arrange
            SetupCatalog([], totalCount: 100);

            // Act
            await _service.GetCatalogAsync(new GetCatalogQuery { PageNumber = pageNumber, PageSize = pageSize });

            // Assert
            _bookRepository.Verify(
                r => r.GetCatalogPageAsync(null, null, CatalogSortBy.Title, expectedSkip, pageSize, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCatalogAsync_FiltersAndSort_PassedToRepository()
        {
            // Arrange
            SetupCatalog([], totalCount: 0);
            var query = new GetCatalogQuery { Genre = "  Поезія ", Year = " 1840 ", SortBy = CatalogSortBy.YearNewest };

            // Act
            await _service.GetCatalogAsync(query);

            // Assert
            _bookRepository.Verify(r => r.CountCatalogAsync("Поезія", 1840, It.IsAny<CancellationToken>()), Times.Once);
            _bookRepository.Verify(
                r => r.GetCatalogPageAsync("Поезія", 1840, CatalogSortBy.YearNewest, 0, 10, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public async Task GetCatalogAsync_EmptyGenreAndYear_SearchesWithoutFilters(string? genre, string? year)
        {
            // Arrange
            SetupCatalog([], totalCount: 0);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Genre = genre, Year = year });

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.CountCatalogAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCatalogAsync_NoMatchingBooks_ReturnsSuccessWithEmptyPage()
        {
            // Arrange
            SetupCatalog([], totalCount: 0);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Genre = "Фентезі" });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Books);
            Assert.Equal(0, result.Value.TotalCount);
            Assert.Equal(0, result.Value.TotalPages);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("18.40")]
        [InlineData("-1840")]
        [InlineData("1 840")]
        public async Task GetCatalogAsync_YearIsNotNumber_ReturnsFailureWithNumberMessage(string year)
        {
            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Year = year });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Рік має бути цілим числом, наприклад 1840.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_YearBeforeMinimum_ReturnsFailureWithRangeMessage()
        {
            // Arrange
            var year = (BookCatalogService.MinCatalogYear - 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Year = year });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік має бути від {BookCatalogService.MinCatalogYear} до {DateTime.Now.Year}.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_YearInFuture_ReturnsFailureWithRangeMessage()
        {
            // Arrange
            var year = (DateTime.Now.Year + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Year = year });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік має бути від {BookCatalogService.MinCatalogYear} до {DateTime.Now.Year}.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_YearOnBoundaries_ReturnsSuccess()
        {
            // Arrange
            SetupCatalog([], totalCount: 0);
            var minYear = BookCatalogService.MinCatalogYear.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var maxYear = DateTime.Now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Act
            var minResult = await _service.GetCatalogAsync(new GetCatalogQuery { Year = minYear });
            var maxResult = await _service.GetCatalogAsync(new GetCatalogQuery { Year = maxYear });

            // Assert
            Assert.True(minResult.IsSuccess);
            Assert.True(maxResult.IsSuccess);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(BookCatalogService.MaxCatalogPageNumber + 1)]
        public async Task GetCatalogAsync_PageNumberOutOfRange_ReturnsFailureWithPageNumberMessage(int pageNumber)
        {
            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { PageNumber = pageNumber });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Номер сторінки має бути від 1 до {BookCatalogService.MaxCatalogPageNumber}.", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(BookCatalogService.MaxCatalogPageSize + 1)]
        public async Task GetCatalogAsync_PageSizeOutOfRange_ReturnsFailureWithPageSizeMessage(int pageSize)
        {
            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { PageSize = pageSize });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Кількість книг на сторінці має бути від 1 до {BookCatalogService.MaxCatalogPageSize}.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_GenreTooLong_ReturnsFailureWithGenreMessage()
        {
            // Arrange
            var tooLongGenre = new string('а', BookCatalogService.MaxCatalogGenreLength + 1);

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { Genre = tooLongGenre });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Жанр не може бути довшим за {BookCatalogService.MaxCatalogGenreLength} символів.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_UnknownSortOrder_ReturnsFailureWithSortMessage()
        {
            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery { SortBy = (CatalogSortBy)99 });

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть спосіб сортування.", result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_InvalidQuery_DoesNotQueryRepository()
        {
            // Act
            await _service.GetCatalogAsync(new GetCatalogQuery { Year = "abc" });

            // Assert
            _bookRepository.Verify(
                r => r.CountCatalogAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _bookRepository.Verify(
                r => r.GetCatalogPageAsync(
                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CatalogSortBy>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetCatalogAsync_RepositoryFails_ReturnsFailureWithLoadFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.CountCatalogAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetCatalogAsync(new GetCatalogQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(LoadFailedMessage, result.Error);
        }

        [Fact]
        public async Task GetCatalogAsync_LoadingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            _bookRepository
                .Setup(r => r.CountCatalogAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.GetCatalogAsync(new GetCatalogQuery()));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task GetCatalogAsync_NullQuery_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _service.GetCatalogAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task GetGenresAsync_GenresExist_ReturnsSuccessWithGenres()
        {
            // Arrange
            IReadOnlyList<string> genres = ["Драма", "Поезія"];
            _bookRepository.Setup(r => r.GetGenresAsync(It.IsAny<CancellationToken>())).ReturnsAsync(genres);

            // Act
            var result = await _service.GetGenresAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(genres, result.Value);
        }

        [Fact]
        public async Task GetGenresAsync_RepositoryFails_ReturnsFailureWithGenresMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetGenresAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.GetGenresAsync();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Не вдалося завантажити список жанрів. Спробуйте пізніше.", result.Error);
        }

        private void SetupCatalog(IReadOnlyList<Book> books, int totalCount)
        {
            _bookRepository
                .Setup(r => r.CountCatalogAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(totalCount);
            _bookRepository
                .Setup(r => r.GetCatalogPageAsync(
                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CatalogSortBy>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);
        }

        private static Book CreateBook(int id, string title)
        {
            return new Book { Id = id, Title = title, Author = "Автор", Genre = "Поезія", Year = 1840 };
        }
    }
}
