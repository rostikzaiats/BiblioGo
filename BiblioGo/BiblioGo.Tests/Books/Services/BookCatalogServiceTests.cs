using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task SearchByTitleAsync_ExistingTitle_ReturnsSuccessWithBooksFromRepository()
        {
            // Arrange
            IReadOnlyList<Book> expectedBooks = [CreateBook(1, ValidTitle), CreateBook(2, "Кобзар. Вибране")];
            var stubRepository = CreateRepositoryReturning(expectedBooks);
            var service = CreateService(stubRepository.Object);

            // Act
            var result = await service.SearchByTitleAsync(ValidTitle);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedBooks, result.Value);
        }

        [Fact]
        public async Task SearchByTitleAsync_NoMatchingBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning([]);
            var service = CreateService(stubRepository.Object);

            // Act
            var result = await service.SearchByTitleAsync(ValidTitle);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
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
        public async Task SearchByTitleAsync_TitleLengthOnBoundary_ReturnsSuccess(int titleLength)
        {
            // Arrange
            var title = new string(TitleCharacter, titleLength);
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            var result = await service.SearchByTitleAsync(title);

            // Assert
            Assert.True(result.IsSuccess);
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(title, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchByTitleAsync_TitleIsMissing_ReturnsFailureWithEnterTitleMessage(string? title)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByTitleAsync(title);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Введіть назву книги для пошуку.", result.Error);
        }

        [Theory]
        [InlineData(TooShortTitle)]
        [InlineData("  К  ")]
        public async Task SearchByTitleAsync_TitleTooShort_ReturnsFailureWithMinLengthMessage(string title)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByTitleAsync(title);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(
                $"Назва для пошуку має містити щонайменше {BookCatalogService.MinSearchTitleLength} символи.",
                result.Error);
        }

        [Fact]
        public async Task SearchByTitleAsync_TitleLongerThanMaximum_ReturnsFailureWithMaxLengthMessage()
        {
            // Arrange
            var tooLongTitle = new string(TitleCharacter, BookCatalogService.MaxSearchTitleLength + 1);
            var service = CreateService(Mock.Of<IBookRepository>());

            // Act
            var result = await service.SearchByTitleAsync(tooLongTitle);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(
                $"Назва для пошуку не може бути довшою за {BookCatalogService.MaxSearchTitleLength} символів.",
                result.Error);
        }

        [Fact]
        public async Task SearchByTitleAsync_InvalidTitle_DoesNotQueryRepository()
        {
            // Arrange
            var mockRepository = new Mock<IBookRepository>();
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByTitleAsync(TooShortTitle);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
        public async Task SearchByTitleAsync_SearchCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            var stubRepository = CreateRepositoryThrowing(canceledException);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.SearchByTitleAsync(ValidTitle));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task SearchByTitleAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var mockRepository = CreateRepositoryReturning([]);
            var service = CreateService(mockRepository.Object);

            // Act
            await service.SearchByTitleAsync(ValidTitle, cancellationTokenSource.Token);

            // Assert
            mockRepository.Verify(
                repository => repository.SearchByTitleAsync(ValidTitle, cancellationTokenSource.Token),
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
