using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Знайти книгу за назвою» (BookCatalogService.SearchByTitleAsync)
    public class BookCatalogServiceTests
    {
        private const string ValidTitle = "Кобзар";
        private const string TooShortTitle = "К";
        private const char TitleCharacter = 'а';

        [Fact]
        public async Task SearchByTitleAsync_ExistingTitle_ReturnsBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<Book> expectedBooks = [CreateBook(1, ValidTitle), CreateBook(2, "Кобзар. Вибране")];
            var stubRepository = CreateRepositoryReturning(expectedBooks);
            var service = CreateService(stubRepository.Object);

            // Act
            var books = await service.SearchByTitleAsync(ValidTitle);

            // Assert
            Assert.Equal(expectedBooks, books);
        }

        [Fact]
        public async Task SearchByTitleAsync_NoMatchingBooks_ReturnsEmptyList()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning([]);
            var service = CreateService(stubRepository.Object);

            // Act
            var books = await service.SearchByTitleAsync(ValidTitle);

            // Assert
            Assert.Empty(books);
        }

        [Theory]
        [InlineData("  Кобзар")]
        [InlineData("Кобзар  ")]
        [InlineData("\tКобзар\r\n")]
        public async Task SearchByTitleAsync_TitleWithSurroundingWhitespace_SearchesByTrimmedTitle(string title)
        {
            // Arrange
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByTitleAsync(title);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(ValidTitle, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(BookCatalogService.MinSearchTitleLength)]
        [InlineData(BookCatalogService.MaxSearchTitleLength)]
        public async Task SearchByTitleAsync_TitleLengthOnBoundary_SearchesInRepository(int titleLength)
        {
            // Arrange
            var title = new string(TitleCharacter, titleLength);
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByTitleAsync(title);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(title, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(TooShortTitle)]
        [InlineData("  К  ")]
        public async Task SearchByTitleAsync_TitleIsMissingOrTooShort_ThrowsInputValidationException(string? title)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByTitleAsync(title));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task SearchByTitleAsync_TitleLongerThanMaximum_ThrowsInputValidationException()
        {
            // Arrange
            var tooLongTitle = new string(TitleCharacter, BookCatalogService.MaxSearchTitleLength + 1);
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByTitleAsync(tooLongTitle));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task SearchByTitleAsync_InvalidTitle_DoesNotQueryRepository()
        {
            // Arrange
            var mockRepository = new Mock<IBookRepository>();
            var service = CreateService(mockRepository.Object);

            // Act
            await Record.ExceptionAsync(() => service.SearchByTitleAsync(TooShortTitle));

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchByTitleAsync_InvalidTitle_LogsWarning()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var service = CreateService(Mock.Of<IBookRepository>(), logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByTitleAsync(TooShortTitle));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task SearchByTitleAsync_ValidTitle_LogsFoundBooksCount()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryReturning([CreateBook(1, ValidTitle)]);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await service.SearchByTitleAsync(ValidTitle);

            // Assert
            Assert.Equal("1", logger.LatestRecord.GetStructuredStateValue("BookCount"));
        }

        [Fact]
        public async Task SearchByTitleAsync_RepositoryFails_RethrowsRepositoryException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var stubRepository = CreateRepositoryThrowing(repositoryException);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByTitleAsync(ValidTitle));

            // Assert
            Assert.Same(repositoryException, exception);
        }

        [Fact]
        public async Task SearchByTitleAsync_RepositoryFails_LogsErrorWithException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryThrowing(repositoryException);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByTitleAsync(ValidTitle));

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task SearchByTitleAsync_SearchCanceled_DoesNotLogError()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryThrowing(new OperationCanceledException());
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByTitleAsync(ValidTitle));

            // Assert
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        private static BookCatalogService CreateService(IBookRepository repository, ILogger<BookCatalogService>? logger = null)
        {
            return new BookCatalogService(repository, logger ?? NullLogger<BookCatalogService>.Instance);
        }

        private static Mock<IBookRepository> CreateRepositoryReturning(IReadOnlyList<Book> books)
        {
            var repository = new Mock<IBookRepository>();
            repository
                .Setup(r => r.SearchByTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);

            return repository;
        }

        private static Mock<IBookRepository> CreateRepositoryThrowing(Exception exception)
        {
            var repository = new Mock<IBookRepository>();
            repository
                .Setup(r => r.SearchByTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            return repository;
        }

        private static Book CreateBook(int id, string title)
        {
            return new Book { Id = id, Title = title, Author = "Тарас Шевченко", Genre = "Поезія", Year = 1840 };
        }
    }
}
