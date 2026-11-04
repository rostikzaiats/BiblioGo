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
    // Unit-тести юз кейсу «Оформити повернення книги» (LoanService.ReturnBookAsync)
    public class LoanServiceReturnBookTests
    {
        private const int LoanId = 5;
        private const int LibrarianId = 2;
        private const int BookCopyId = 10;

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task ReturnBookAsync_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnBookAsync_ValidCommand_ReturnsLoanAndItsCopy()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.ReturnAsync(LoanId, BookCopyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ReturnBookAsync_ValidCommand_SetsReturnDateToNowInUtc()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.ReturnAsync(
                    LoanId,
                    BookCopyId,
                    It.Is<DateTime>(returnDate => returnDate.Kind == DateTimeKind.Utc
                        && DateTime.UtcNow - returnDate < TimeSpan.FromMinutes(1)),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ReturnBookAsync_OverdueLoan_ReturnsSuccess()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Loan
                {
                    Id = LoanId,
                    BookCopyId = BookCopyId,
                    Status = LoanStatus.Issued,
                    DueDate = DateTime.UtcNow.AddDays(-30)
                });
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnBookAsync_ValidCommand_LogsLoanReturned()
        {
            // Arrange
            SetupHappyPath();
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task ReturnBookAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.ReturnBookAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [InlineData(0, LibrarianId)]
        [InlineData(-1, LibrarianId)]
        [InlineData(LoanId, 0)]
        [InlineData(LoanId, -1)]
        public async Task ReturnBookAsync_InvalidIds_ReturnsFailure(int loanId, int librarianId)
        {
            // Arrange
            var command = new ReturnBookCommand { LoanId = loanId, LibrarianId = librarianId };
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ReturnBookAsync_InvalidCommand_DoesNotCallRepositories()
        {
            // Arrange
            var command = new ReturnBookCommand { LoanId = 0, LibrarianId = LibrarianId };
            var sut = CreateService();

            // Act
            await sut.ReturnBookAsync(command);

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _loanRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ReturnBookAsync_InvalidCommand_LogsWarning()
        {
            // Arrange
            var command = new ReturnBookCommand { LoanId = 0, LibrarianId = LibrarianId };
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ReturnBookAsync(command);

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task ReturnBookAsync_LibrarianNotFound_ReturnsFailureAndDoesNotReturn()
        {
            // Arrange
            SetupHappyPath();
            _userRepository.Setup(r => r.FindByIdAsync(LibrarianId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            VerifyBookNotReturned();
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Administrator)]
        public async Task ReturnBookAsync_UserIsNotLibrarian_ReturnsFailureAndDoesNotReturn(UserRole role)
        {
            // Arrange
            SetupHappyPath();
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = role });
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyBookNotReturned();
        }

        [Fact]
        public async Task ReturnBookAsync_LoanNotFound_ReturnsFailureAndDoesNotReturn()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Loan?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyBookNotReturned();
        }

        [Theory]
        [InlineData(LoanStatus.Requested)]
        [InlineData(LoanStatus.Returned)]
        public async Task ReturnBookAsync_LoanIsNotIssued_ReturnsFailureAndDoesNotReturn(LoanStatus status)
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Loan { Id = LoanId, BookCopyId = BookCopyId, Status = status });
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyBookNotReturned();
        }

        [Fact]
        public async Task ReturnBookAsync_StateChangedConcurrently_ReturnsFailure()
        {
            // Arrange
            SetupHappyPath();
            SetupReturnResult(false);
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ReturnBookAsync_StateChangedConcurrently_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            SetupReturnResult(false);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ReturnBookAsync_BusinessRuleViolation_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Loan { Id = LoanId, BookCopyId = BookCopyId, Status = LoanStatus.Returned });
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ReturnBookAsync_RepositoryFails_ReturnsFailureWithoutTechnicalDetails()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.ReturnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));
            var sut = CreateService();

            // Act
            var result = await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.DoesNotContain("Database is unavailable.", result.Error);
        }

        [Fact]
        public async Task ReturnBookAsync_RepositoryFails_LogsErrorWithException()
        {
            // Arrange
            SetupHappyPath();
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            _loanRepository
                .Setup(r => r.ReturnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ReturnBookAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task ReturnBookAsync_RequestCanceled_ThrowsOperationCanceledAndDoesNotLogError()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.ReturnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            var exception = await Record.ExceptionAsync(() => sut.ReturnBookAsync(CreateValidCommand()));

            // Assert
            Assert.IsType<OperationCanceledException>(exception);
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

        // Бібліотекар існує, книга видана читачу, повернення проходить успішно
        private void SetupHappyPath()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = UserRole.Librarian });
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Loan { Id = LoanId, BookCopyId = BookCopyId, Status = LoanStatus.Issued });
            SetupReturnResult(true);
        }

        private void SetupReturnResult(bool returned)
        {
            _loanRepository
                .Setup(r => r.ReturnAsync(LoanId, BookCopyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returned);
        }

        private void VerifyBookNotReturned()
        {
            _loanRepository.Verify(
                r => r.ReturnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static ReturnBookCommand CreateValidCommand()
        {
            return new ReturnBookCommand { LoanId = LoanId, LibrarianId = LibrarianId };
        }
    }
}
