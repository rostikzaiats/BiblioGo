using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task SearchByAuthorAsync_ExistingAuthor_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<Book> expectedBooks = [CreateBook(1, "Кобзар"), CreateBook(2, "Гайдамаки")];
            var stubRepository = CreateRepositoryReturning(expectedBooks);
            var service = CreateService(stubRepository.Object);

            // Act
            var result = await service.SearchByAuthorAsync(ValidAuthor);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedBooks, result.Value);
        }

        [Fact]
        public async Task SearchByAuthorAsync_NoMatchingBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning([]);
            var service = CreateService(stubRepository.Object);

            // Act
            var result = await service.SearchByAuthorAsync(ValidAuthor);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
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
        public async Task SearchByAuthorAsync_AuthorLengthOnBoundary_ReturnsSuccess(int authorLength)
        {
            // Arrange
            var author = new string(AuthorCharacter, authorLength);
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            var result = await service.SearchByAuthorAsync(author);

            // Assert
            Assert.True(result.IsSuccess);
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(author, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchByAuthorAsync_AuthorIsMissing_ReturnsFailureWithEnterAuthorMessage(string? author)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByAuthorAsync(author);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Введіть ім'я або прізвище автора для пошуку.", result.Error);
        }

        [Theory]
        [InlineData(TooShortAuthor)]
        [InlineData("  Ш  ")]
        public async Task SearchByAuthorAsync_AuthorTooShort_ReturnsFailureWithMinLengthMessage(string author)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByAuthorAsync(author);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(
                $"Ім'я автора для пошуку має містити щонайменше {BookCatalogService.MinSearchAuthorLength} символи.",
                result.Error);
        }

        [Fact]
        public async Task SearchByAuthorAsync_AuthorLongerThanMaximum_ReturnsFailureWithMaxLengthMessage()
        {
            // Arrange
            var tooLongAuthor = new string(AuthorCharacter, BookCatalogService.MaxSearchAuthorLength + 1);
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByAuthorAsync(tooLongAuthor);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(
                $"Ім'я автора для пошуку не може бути довшим за {BookCatalogService.MaxSearchAuthorLength} символів.",
                result.Error);
        }

        [Fact]
        public async Task SearchByAuthorAsync_InvalidAuthor_DoesNotQueryRepository()
        {
            // Arrange
            var mockRepository = new Mock<IBookRepository>();
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByAuthorAsync(TooShortAuthor);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
        public async Task SearchByAuthorAsync_SearchCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            var stubRepository = CreateRepositoryThrowing(canceledException);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByAuthorAsync(ValidAuthor));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task SearchByAuthorAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByAuthorAsync(ValidAuthor, cancellationTokenSource.Token);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByAuthorAsync(ValidAuthor, cancellationTokenSource.Token),
                Times.Once);
        }

        private static BookCatalogService CreateService(IBookRepository repository)
        {
            return new BookCatalogService(repository, NullLogger<BookCatalogService>.Instance);
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
