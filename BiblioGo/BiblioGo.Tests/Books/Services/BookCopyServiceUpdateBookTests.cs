using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Редагувати інформацію про книгу» (BookCopyService.UpdateBookAsync)
    public class BookCopyServiceUpdateBookTests
    {
        private const int BookId = 4;
        private const string ValidTitle = "Кобзар";
        private const string ValidAuthor = "Тарас Шевченко";
        private const string ValidGenre = "Поезія";
        private const int ValidYear = 1840;
        private const string ValidDescription = "Збірка поетичних творів.";
        private const char FillerCharacter = 'а';
        private const string DuplicateMessage = "Книга з такою назвою та автором уже є в каталозі.";

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceUpdateBookTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task UpdateBookAsync_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            SetupUpdate(updated: true);

            // Act
            var result = await _service.UpdateBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_ValidCommand_SavesBookWithCommandData()
        {
            // Arrange
            SetupUpdate(updated: true);

            // Act
            await _service.UpdateBookAsync(CreateCommand());

            // Assert
            _bookRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<Book>(book => book.Id == BookId
                        && book.Title == ValidTitle
                        && book.Author == ValidAuthor
                        && book.Genre == ValidGenre
                        && book.Year == ValidYear
                        && book.Description == ValidDescription),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateBookAsync_FieldsWithSurroundingWhitespace_SavesTrimmedValues()
        {
            // Arrange
            SetupUpdate(updated: true);
            var command = CreateCommand(
                title: "  Кобзар ",
                author: "\tТарас Шевченко  ",
                genre: " Поезія ",
                description: "  Збірка поетичних творів.\r\n");

            // Act
            await _service.UpdateBookAsync(command);

            // Assert
            _bookRepository.Verify(
                r => r.ExistsAnotherAsync(ValidTitle, ValidAuthor, BookId, It.IsAny<CancellationToken>()),
                Times.Once);
            _bookRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<Book>(book => book.Title == ValidTitle
                        && book.Author == ValidAuthor
                        && book.Genre == ValidGenre
                        && book.Description == ValidDescription),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateBookAsync_EmptyDescription_ReturnsSuccess()
        {
            // Arrange
            SetupUpdate(updated: true);

            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(description: string.Empty));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UpdateBookAsync_FieldsOnMaximumLength_ReturnsSuccess()
        {
            // Arrange
            SetupUpdate(updated: true);
            var command = CreateCommand(
                title: new string(FillerCharacter, 200),
                author: new string(FillerCharacter, 100),
                genre: new string(FillerCharacter, 50),
                description: new string(FillerCharacter, 2000));

            // Act
            var result = await _service.UpdateBookAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task UpdateBookAsync_BookNotSelected_ReturnsFailureWithSelectBookMessage(int bookId)
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(bookId: bookId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть книгу", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateBookAsync_TitleIsMissing_ReturnsFailureWithRequiredMessage(string title)
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(title: title));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть назву книги", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_TitleLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(title: new string(FillerCharacter, 201)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Назва книги не може бути довшою за 200 символів", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateBookAsync_AuthorIsMissing_ReturnsFailureWithRequiredMessage(string author)
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(author: author));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть автора книги", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_AuthorLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(author: new string(FillerCharacter, 101)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Ім'я автора не може бути довшим за 100 символів", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateBookAsync_GenreIsMissing_ReturnsFailureWithRequiredMessage(string genre)
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(genre: genre));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть жанр книги", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_GenreLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(genre: new string(FillerCharacter, 51)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Жанр не може бути довшим за 50 символів", result.Error);
        }

        [Theory]
        [InlineData(999)]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task UpdateBookAsync_YearBeforeMinimum_ReturnsFailureWithYearRangeMessage(int year)
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(year: year));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік видання має бути від 1000 до {DateTime.Now.Year}", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_YearInTheFuture_ReturnsFailureWithYearRangeMessage()
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(year: DateTime.Now.Year + 1));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік видання має бути від 1000 до {DateTime.Now.Year}", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_DescriptionLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(description: new string(FillerCharacter, 2001)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Опис не може бути довшим за 2000 символів", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_InvalidCommand_DoesNotQueryRepository()
        {
            // Act
            await _service.UpdateBookAsync(CreateCommand(title: string.Empty));

            // Assert
            _bookRepository.Verify(
                r => r.ExistsAnotherAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
            VerifyBookNeverUpdated();
        }

        [Fact]
        public async Task UpdateBookAsync_AnotherBookHasSameTitleAndAuthor_ReturnsFailureWithDuplicateMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.ExistsAnotherAsync(ValidTitle, ValidAuthor, BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DuplicateMessage, result.Error);
            VerifyBookNeverUpdated();
        }

        [Fact]
        public async Task UpdateBookAsync_BookDoesNotExist_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupUpdate(updated: false);

            // Act
            var result = await _service.UpdateBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книгу не знайдено.", result.Error);
        }

        [Fact]
        public async Task UpdateBookAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            _bookRepository.Setup(r => r.ExistsAnotherAsync(ValidTitle, ValidAuthor, BookId, token)).ReturnsAsync(false);
            _bookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), token)).ReturnsAsync(true);

            // Act
            var result = await _service.UpdateBookAsync(CreateCommand(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.ExistsAnotherAsync(ValidTitle, ValidAuthor, BookId, token), Times.Once);
            _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), token), Times.Once);
        }

        private void SetupUpdate(bool updated)
        {
            _bookRepository
                .Setup(r => r.ExistsAnotherAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _bookRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated);
        }

        private void VerifyBookNeverUpdated()
        {
            _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static UpdateBookCommand CreateCommand(
            int bookId = BookId,
            string title = ValidTitle,
            string author = ValidAuthor,
            string genre = ValidGenre,
            int year = ValidYear,
            string description = ValidDescription)
        {
            return new UpdateBookCommand
            {
                BookId = bookId,
                Title = title,
                Author = author,
                Genre = genre,
                Year = year,
                Description = description
            };
        }
    }
}
