using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Commands;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace BiblioGo.Tests.Loans.Services
{
    // Unit-тести юз кейсу «Оформити заявку на отримання книги» (LoanService.CreateRequestAsync)
    public class LoanServiceTests
    {
        private const int UserId = 1;
        private const int BookId = 2;
        private const int LibraryId = 3;
        private const int BookCopyId = 10;

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task CreateRequestAsync_ValidCommand_ReturnsNewLoanId()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Loan loan, CancellationToken _) =>
                {
                    loan.Id = 42;
                    return loan;
                });
            var sut = CreateService();

            // Act
            var result = await sut.CreateRequestAsync(CreateValidCommand());

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task CreateRequestAsync_ValidCommand_SavesLoanWithRequestedStatusForFoundCopy()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.CreateRequestAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.AddAsync(
                    It.Is<Loan>(l => l.UserId == UserId
                        && l.BookCopyId == BookCopyId
                        && l.Status == LoanStatus.Requested
                        && l.ReturnDate == null),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task CreateRequestAsync_ValidCommand_SavesLoanWithUtcDates()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.CreateRequestAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.AddAsync(
                    It.Is<Loan>(l => l.IssueDate.Kind == DateTimeKind.Utc && l.DueDate.Kind == DateTimeKind.Utc),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task CreateRequestAsync_ValidCommand_SearchesCopyInRequestedLibrary()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.CreateRequestAsync(CreateValidCommand());

            // Assert
            _bookCopyRepository.Verify(
                r => r.FindAvailableCopyAsync(BookId, LibraryId, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task CreateRequestAsync_ValidCommand_LogsLoanCreated()
        {
            // Arrange
            SetupHappyPath();
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.CreateRequestAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task CreateRequestAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [InlineData(0, BookId, LibraryId)]
        [InlineData(-1, BookId, LibraryId)]
        [InlineData(UserId, 0, LibraryId)]
        [InlineData(UserId, -5, LibraryId)]
        [InlineData(UserId, BookId, 0)]
        [InlineData(UserId, BookId, -1)]
        public async Task CreateRequestAsync_InvalidIds_ThrowsInputValidationException(int userId, int bookId, int libraryId)
        {
            // Arrange
            var command = new CreateLoanRequestCommand { UserId = userId, BookId = bookId, LibraryId = libraryId };
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(command));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task CreateRequestAsync_InvalidCommand_DoesNotCallRepositories()
        {
            // Arrange
            var command = new CreateLoanRequestCommand { UserId = 0, BookId = BookId, LibraryId = LibraryId };
            var sut = CreateService();

            // Act
            await Record.ExceptionAsync(() => sut.CreateRequestAsync(command));

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _loanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateRequestAsync_InvalidCommand_LogsWarning()
        {
            // Arrange
            var command = new CreateLoanRequestCommand { UserId = 0, BookId = BookId, LibraryId = LibraryId };
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await Record.ExceptionAsync(() => sut.CreateRequestAsync(command));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task CreateRequestAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            SetupHappyPath();
            _userRepository.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.IsType<NotFoundException>(exception);
            _loanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateRequestAsync_UserAlreadyHasActiveLoanForBook_ThrowsBusinessRuleException()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.HasActiveLoanForBookAsync(UserId, BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.IsType<BusinessRuleException>(exception);
            _loanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateRequestAsync_NoAvailableCopy_ThrowsBusinessRuleException()
        {
            // Arrange
            SetupHappyPath();
            _bookCopyRepository
                .Setup(r => r.FindAvailableCopyAsync(BookId, LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BookCopy?)null);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.IsType<BusinessRuleException>(exception);
            _loanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateRequestAsync_BusinessRuleViolation_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            _bookCopyRepository
                .Setup(r => r.FindAvailableCopyAsync(BookId, LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BookCopy?)null);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task CreateRequestAsync_RepositoryFails_RethrowsRepositoryException()
        {
            // Arrange
            SetupHappyPath();
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            _loanRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.Same(repositoryException, exception);
        }

        [Fact]
        public async Task CreateRequestAsync_RepositoryFails_LogsErrorWithException()
        {
            // Arrange
            SetupHappyPath();
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            _loanRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task CreateRequestAsync_RequestCanceled_DoesNotLogError()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await Record.ExceptionAsync(() => sut.CreateRequestAsync(CreateValidCommand()));

            // Assert
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        private LoanService CreateService(ILogger<LoanService>? logger = null)
        {
            return new LoanService(
                _loanRepository.Object,
                _bookCopyRepository.Object,
                _userRepository.Object,
                logger ?? NullLogger<LoanService>.Instance);
        }

        // Користувач існує, активних позик на книгу немає, вільний примірник знайдено
        private void SetupHappyPath()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(UserId))
                .ReturnsAsync(new User { Id = UserId, Role = UserRole.Reader });
            _loanRepository
                .Setup(r => r.HasActiveLoanForBookAsync(UserId, BookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _bookCopyRepository
                .Setup(r => r.FindAvailableCopyAsync(BookId, LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BookCopy
                {
                    Id = BookCopyId,
                    BookId = BookId,
                    LibraryId = LibraryId,
                    Status = BookCopyStatus.Available
                });
            _loanRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Loan loan, CancellationToken _) => loan);
        }

        private static CreateLoanRequestCommand CreateValidCommand()
        {
            return new CreateLoanRequestCommand { UserId = UserId, BookId = BookId, LibraryId = LibraryId };
        }
    }
}
