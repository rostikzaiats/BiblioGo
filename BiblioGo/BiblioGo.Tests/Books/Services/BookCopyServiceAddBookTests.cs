using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Додати нову книгу до каталогу» (BookCopyService.AddBookAsync)
    public class BookCopyServiceAddBookTests
    {
        private const int NewBookId = 15;
        private const string ValidTitle = "Кобзар";
        private const string ValidAuthor = "Тарас Шевченко";
        private const string ValidGenre = "Поезія";
        private const int ValidYear = 1840;
        private const string ValidDescription = "Збірка поетичних творів.";
        private const char FillerCharacter = 'а';
        private const string AddFailedMessage = "Не вдалося додати книгу. Спробуйте пізніше.";

        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly BookCopyService _service;

        public BookCopyServiceAddBookTests()
        {
            _service = new BookCopyService(
                _bookRepository.Object,
                _bookCopyRepository.Object,
                _libraryRepository.Object,
                NullLogger<BookCopyService>.Instance);
        }

        [Fact]
        public async Task AddBookAsync_ValidCommand_ReturnsSuccessWithNewBookId()
        {
            // Arrange
            SetupAdd();

            // Act
            var result = await _service.AddBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewBookId, result.Value);
        }

        [Fact]
        public async Task AddBookAsync_ValidCommand_SavesBookWithCommandData()
        {
            // Arrange
            SetupAdd();

            // Act
            await _service.AddBookAsync(CreateCommand());

            // Assert
            _bookRepository.Verify(
                r => r.AddAsync(
                    It.Is<Book>(book => book.Title == ValidTitle
                        && book.Author == ValidAuthor
                        && book.Genre == ValidGenre
                        && book.Year == ValidYear
                        && book.Description == ValidDescription),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddBookAsync_FieldsWithSurroundingWhitespace_SavesTrimmedValues()
        {
            // Arrange
            SetupAdd();
            var command = CreateCommand(
                title: "  Кобзар ",
                author: "\tТарас Шевченко  ",
                genre: " Поезія ",
                description: "  Збірка поетичних творів.\r\n");

            // Act
            await _service.AddBookAsync(command);

            // Assert
            _bookRepository.Verify(
                r => r.ExistsAsync(ValidTitle, ValidAuthor, It.IsAny<CancellationToken>()),
                Times.Once);
            _bookRepository.Verify(
                r => r.AddAsync(
                    It.Is<Book>(book => book.Title == ValidTitle
                        && book.Author == ValidAuthor
                        && book.Genre == ValidGenre
                        && book.Description == ValidDescription),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddBookAsync_EmptyDescription_ReturnsSuccess()
        {
            // Arrange
            SetupAdd();

            // Act
            var result = await _service.AddBookAsync(CreateCommand(description: string.Empty));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddBookAsync_FieldsOnMaximumLength_ReturnsSuccess()
        {
            // Arrange
            SetupAdd();
            var command = CreateCommand(
                title: new string(FillerCharacter, 200),
                author: new string(FillerCharacter, 100),
                genre: new string(FillerCharacter, 50),
                description: new string(FillerCharacter, 2000));

            // Act
            var result = await _service.AddBookAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddBookAsync_YearOnLowerBoundary_ReturnsSuccess()
        {
            // Arrange
            SetupAdd();

            // Act
            var result = await _service.AddBookAsync(CreateCommand(year: 1000));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddBookAsync_YearIsCurrentYear_ReturnsSuccess()
        {
            // Arrange
            SetupAdd();

            // Act
            var result = await _service.AddBookAsync(CreateCommand(year: DateTime.Now.Year));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddBookAsync_TitleIsMissing_ReturnsFailureWithRequiredMessage(string title)
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(title: title));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть назву книги", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_TitleLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(title: new string(FillerCharacter, 201)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Назва книги не може бути довшою за 200 символів", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddBookAsync_AuthorIsMissing_ReturnsFailureWithRequiredMessage(string author)
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(author: author));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть автора книги", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_AuthorLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(author: new string(FillerCharacter, 101)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Ім'я автора не може бути довшим за 100 символів", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddBookAsync_GenreIsMissing_ReturnsFailureWithRequiredMessage(string genre)
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(genre: genre));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть жанр книги", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_GenreLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(genre: new string(FillerCharacter, 51)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Жанр не може бути довшим за 50 символів", result.Error);
        }

        [Theory]
        [InlineData(999)]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task AddBookAsync_YearBeforeMinimum_ReturnsFailureWithYearRangeMessage(int year)
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(year: year));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік видання має бути від 1000 до {DateTime.Now.Year}", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_YearInTheFuture_ReturnsFailureWithYearRangeMessage()
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(year: DateTime.Now.Year + 1));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal($"Рік видання має бути від 1000 до {DateTime.Now.Year}", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_DescriptionLongerThanMaximum_ReturnsFailureWithTooLongMessage()
        {
            // Act
            var result = await _service.AddBookAsync(CreateCommand(description: new string(FillerCharacter, 2001)));

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Опис не може бути довшим за 2000 символів", result.Error);
        }

        [Fact]
        public async Task AddBookAsync_InvalidCommand_DoesNotQueryRepository()
        {
            // Act
            await _service.AddBookAsync(CreateCommand(title: string.Empty));

            // Assert
            _bookRepository.Verify(
                r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            VerifyBookNeverAdded();
        }

        [Fact]
        public async Task AddBookAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _service.AddBookAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task AddBookAsync_BookAlreadyInCatalog_ReturnsFailureWithDuplicateMessage()
        {
            // Arrange
            SetupExists(true);

            // Act
            var result = await _service.AddBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Книга з такою назвою та автором уже є в каталозі.", result.Error);
            VerifyBookNeverAdded();
        }

        [Fact]
        public async Task AddBookAsync_ExistenceCheckFails_ReturnsFailureWithAddFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.AddBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AddFailedMessage, result.Error);
            VerifyBookNeverAdded();
        }

        [Fact]
        public async Task AddBookAsync_SavingFails_ReturnsFailureWithAddFailedMessage()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));

            // Act
            var result = await _service.AddBookAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AddFailedMessage, result.Error);
        }

        [Fact]
        public async Task AddBookAsync_SavingCanceled_RethrowsOperationCanceledException()
        {
            // Arrange
            var canceledException = new OperationCanceledException();
            _bookRepository
                .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(canceledException);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.AddBookAsync(CreateCommand()));

            // Assert
            Assert.Same(canceledException, exception);
        }

        [Fact]
        public async Task AddBookAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            _bookRepository.Setup(r => r.ExistsAsync(ValidTitle, ValidAuthor, token)).ReturnsAsync(false);
            _bookRepository.Setup(r => r.AddAsync(It.IsAny<Book>(), token)).ReturnsAsync(NewBookId);

            // Act
            var result = await _service.AddBookAsync(CreateCommand(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _bookRepository.Verify(r => r.ExistsAsync(ValidTitle, ValidAuthor, token), Times.Once);
            _bookRepository.Verify(r => r.AddAsync(It.IsAny<Book>(), token), Times.Once);
        }

        private void SetupExists(bool exists)
        {
            _bookRepository
                .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);
        }

        private void SetupAdd()
        {
            SetupExists(false);
            _bookRepository
                .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewBookId);
        }

        private void VerifyBookNeverAdded()
        {
            _bookRepository.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static AddBookCommand CreateCommand(
            string title = ValidTitle,
            string author = ValidAuthor,
            string genre = ValidGenre,
            int year = ValidYear,
            string description = ValidDescription)
        {
            return new AddBookCommand
            {
                Title = title,
                Author = author,
                Genre = genre,
                Year = year,
                Description = description
            };
        }
    }
}
