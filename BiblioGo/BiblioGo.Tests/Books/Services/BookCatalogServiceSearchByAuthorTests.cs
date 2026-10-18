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
    // Unit-тести юз кейсу «Знайти книгу за автором» (BookCatalogService.SearchByAuthorAsync)
    public class BookCatalogServiceSearchByAuthorTests
    {
        private const string ValidAuthor = "Шевченко";
        private const string TooShortAuthor = "Ш";
        private const char AuthorCharacter = 'а';

        [Fact]
        public async Task SearchByAuthorAsync_ExistingAuthor_ReturnsBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<Book> expectedBooks = [CreateBook(1, "Кобзар"), CreateBook(2, "Гайдамаки")];
            var stubRepository = CreateRepositoryReturning(expectedBooks);
            var service = CreateService(stubRepository.Object);

            // Act
            var books = await service.SearchByAuthorAsync(ValidAuthor);

            // Assert
            Assert.Equal(expectedBooks, books);
        }

        [Fact]
        public async Task SearchByAuthorAsync_NoMatchingBooks_ReturnsEmptyList()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning([]);
            var service = CreateService(stubRepository.Object);

            // Act
            var books = await service.SearchByAuthorAsync(ValidAuthor);

            // Assert
            Assert.Empty(books);
        }

        [Theory]
        [InlineData("  Шевченко")]
        [InlineData("Шевченко  ")]
        [InlineData("\tШевченко\r\n")]
        public async Task SearchByAuthorAsync_AuthorWithSurroundingWhitespace_SearchesByTrimmedAuthor(string author)
        {
            // Arrange
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByAuthorAsync(author);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(ValidAuthor, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(BookCatalogService.MinSearchAuthorLength)]
        [InlineData(BookCatalogService.MaxSearchAuthorLength)]
        public async Task SearchByAuthorAsync_AuthorLengthOnBoundary_SearchesInRepository(int authorLength)
        {
            // Arrange
            var author = new string(AuthorCharacter, authorLength);
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByAuthorAsync(author);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(author, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(TooShortAuthor)]
        [InlineData("  Ш  ")]
        public async Task SearchByAuthorAsync_AuthorIsMissingOrTooShort_ThrowsInputValidationException(string? author)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByAuthorAsync(author));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task SearchByAuthorAsync_AuthorLongerThanMaximum_ThrowsInputValidationException()
        {
            // Arrange
            var tooLongAuthor = new string(AuthorCharacter, BookCatalogService.MaxSearchAuthorLength + 1);
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByAuthorAsync(tooLongAuthor));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task SearchByAuthorAsync_InvalidAuthor_DoesNotQueryRepository()
        {
            // Arrange
            var mockRepository = new Mock<IBookRepository>();
            var service = CreateService(mockRepository.Object);

            // Act
            await Record.ExceptionAsync(() => service.SearchByAuthorAsync(TooShortAuthor));

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchByAuthorAsync_InvalidAuthor_LogsWarning()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var service = CreateService(Mock.Of<IBookRepository>(), logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByAuthorAsync(TooShortAuthor));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task SearchByAuthorAsync_ValidAuthor_LogsFoundBooksCount()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryReturning([CreateBook(1, "Кобзар")]);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await service.SearchByAuthorAsync(ValidAuthor);

            // Assert
            Assert.Equal("1", logger.LatestRecord.GetStructuredStateValue("BookCount"));
        }

        [Fact]
        public async Task SearchByAuthorAsync_RepositoryFails_RethrowsRepositoryException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var stubRepository = CreateRepositoryThrowing(repositoryException);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByAuthorAsync(ValidAuthor));

            // Assert
            Assert.Same(repositoryException, exception);
        }

        [Fact]
        public async Task SearchByAuthorAsync_RepositoryFails_LogsErrorWithException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryThrowing(repositoryException);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByAuthorAsync(ValidAuthor));

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task SearchByAuthorAsync_SearchCanceled_DoesNotLogError()
        {
            // Arrange
            var logger = new FakeLogger<BookCatalogService>();
            var stubRepository = CreateRepositoryThrowing(new OperationCanceledException());
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.SearchByAuthorAsync(ValidAuthor));

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
                .Setup(r => r.SearchByAuthorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(books);

            return repository;
        }

        private static Mock<IBookRepository> CreateRepositoryThrowing(Exception exception)
        {
            var repository = new Mock<IBookRepository>();
            repository
                .Setup(r => r.SearchByAuthorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            return repository;
        }

        private static Book CreateBook(int id, string title)
        {
            return new Book { Id = id, Title = title, Author = "Тарас Шевченко", Genre = "Поезія", Year = 1840 };
        }
    }
}
