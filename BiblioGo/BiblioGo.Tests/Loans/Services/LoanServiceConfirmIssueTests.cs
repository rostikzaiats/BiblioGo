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
    // Unit-тести юз кейсу «Підтвердити та оформити видачу книги» (LoanService.ConfirmIssueAsync)
    public class LoanServiceConfirmIssueTests
    {
        private const int LoanId = 5;
        private const int LibrarianId = 2;
        private const int BookCopyId = 10;

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task ConfirmIssueAsync_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ConfirmIssueAsync_ValidCommand_ConfirmsIssueForLoanAndItsCopy()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.ConfirmIssueAsync(LoanId, BookCopyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ConfirmIssueAsync_ValidCommand_SetsIssueDateToNowInUtc()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            _loanRepository.Verify(
                r => r.ConfirmIssueAsync(
                    LoanId,
                    BookCopyId,
                    It.Is<DateTime>(issueDate => issueDate.Kind == DateTimeKind.Utc
                        && DateTime.UtcNow - issueDate < TimeSpan.FromMinutes(1)),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ConfirmIssueAsync_ValidCommand_SetsDueDateToEndOfSelectedDayInUtc()
        {
            // Arrange
            SetupHappyPath();
            var command = CreateValidCommand();
            var expectedLocalDueDate = command.DueDate.Date.AddDays(1).AddSeconds(-1);
            var sut = CreateService();

            // Act
            await sut.ConfirmIssueAsync(command);

            // Assert
            _loanRepository.Verify(
                r => r.ConfirmIssueAsync(
                    LoanId,
                    BookCopyId,
                    It.IsAny<DateTime>(),
                    It.Is<DateTime>(dueDate => dueDate.Kind == DateTimeKind.Utc
                        && dueDate.ToLocalTime() == expectedLocalDueDate),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ConfirmIssueAsync_ValidCommand_LogsLoanIssued()
        {
            // Arrange
            SetupHappyPath();
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Theory]
        [InlineData(0, LibrarianId)]
        [InlineData(-1, LibrarianId)]
        [InlineData(LoanId, 0)]
        [InlineData(LoanId, -1)]
        public async Task ConfirmIssueAsync_InvalidIds_ReturnsFailure(int loanId, int librarianId)
        {
            // Arrange
            var command = CreateValidCommand();
            command.LoanId = loanId;
            command.LibrarianId = librarianId;
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task ConfirmIssueAsync_DueDateNotAfterToday_ReturnsFailure(int daysFromToday)
        {
            // Arrange
            var command = CreateValidCommand();
            command.DueDate = DateTime.Today.AddDays(daysFromToday);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ConfirmIssueAsync_InvalidCommand_DoesNotCallRepositories()
        {
            // Arrange
            var command = CreateValidCommand();
            command.LoanId = 0;
            var sut = CreateService();

            // Act
            await sut.ConfirmIssueAsync(command);

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _loanRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ConfirmIssueAsync_InvalidCommand_LogsWarning()
        {
            // Arrange
            var command = CreateValidCommand();
            command.LoanId = 0;
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ConfirmIssueAsync(command);

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task ConfirmIssueAsync_LibrarianNotFound_ReturnsFailureAndDoesNotConfirm()
        {
            // Arrange
            SetupHappyPath();
            _userRepository.Setup(r => r.FindByIdAsync(LibrarianId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            VerifyIssueNotConfirmed();
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Administrator)]
        public async Task ConfirmIssueAsync_UserIsNotLibrarian_ReturnsFailureAndDoesNotConfirm(UserRole role)
        {
            // Arrange
            SetupHappyPath();
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = role });
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyIssueNotConfirmed();
        }

        [Fact]
        public async Task ConfirmIssueAsync_LoanNotFound_ReturnsFailureAndDoesNotConfirm()
        {
            // Arrange
            SetupHappyPath();
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Loan?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyIssueNotConfirmed();
        }

        [Theory]
        [InlineData(LoanStatus.Issued)]
        [InlineData(LoanStatus.Returned)]
        public async Task ConfirmIssueAsync_LoanIsNotRequested_ReturnsFailureAndDoesNotConfirm(LoanStatus status)
        {
            // Arrange
            SetupHappyPath();
            SetupLoan(status);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyIssueNotConfirmed();
        }

        [Fact]
        public async Task ConfirmIssueAsync_CopyNotFound_ReturnsFailureAndDoesNotConfirm()
        {
            // Arrange
            SetupHappyPath();
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BookCopy?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyIssueNotConfirmed();
        }

        [Theory]
        [InlineData(BookCopyStatus.Issued)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task ConfirmIssueAsync_CopyIsNotAvailable_ReturnsFailureAndDoesNotConfirm(BookCopyStatus status)
        {
            // Arrange
            SetupHappyPath();
            SetupCopy(status);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyIssueNotConfirmed();
        }

        [Fact]
        public async Task ConfirmIssueAsync_StateChangedConcurrently_ReturnsFailure()
        {
            // Arrange
            SetupHappyPath();
            SetupConfirmIssueResult(false);
            var sut = CreateService();

            // Act
            var result = await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ConfirmIssueAsync_StateChangedConcurrently_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            SetupConfirmIssueResult(false);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ConfirmIssueAsync_BusinessRuleViolation_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            SetupLoan(LoanStatus.Issued);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ConfirmIssueAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
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

        // Бібліотекар існує, заявка очікує підтвердження, примірник вільний, видача проходить успішно
        private void SetupHappyPath()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = UserRole.Librarian });
            SetupLoan(LoanStatus.Requested);
            SetupCopy(BookCopyStatus.Available);
            SetupConfirmIssueResult(true);
        }

        private void SetupLoan(LoanStatus status)
        {
            _loanRepository
                .Setup(r => r.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Loan { Id = LoanId, BookCopyId = BookCopyId, Status = status });
        }

        private void SetupCopy(BookCopyStatus status)
        {
            _bookCopyRepository
                .Setup(r => r.GetByIdAsync(BookCopyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BookCopy { Id = BookCopyId, Status = status });
        }

        private void SetupConfirmIssueResult(bool confirmed)
        {
            _loanRepository
                .Setup(r => r.ConfirmIssueAsync(LoanId, BookCopyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(confirmed);
        }

        private void VerifyIssueNotConfirmed()
        {
            _loanRepository.Verify(
                r => r.ConfirmIssueAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static ConfirmLoanIssueCommand CreateValidCommand()
        {
            return new ConfirmLoanIssueCommand
            {
                LoanId = LoanId,
                LibrarianId = LibrarianId,
                DueDate = DateTime.Today.AddDays(14)
            };
        }
    }
}
