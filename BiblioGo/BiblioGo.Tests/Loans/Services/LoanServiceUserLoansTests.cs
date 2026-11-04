using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace BiblioGo.Tests.Loans.Services
{
    // Unit-тести юз кейсу «Переглянути власні позики та терміни» (LoanService.GetUserLoansAsync)
    public class LoanServiceUserLoansTests
    {
        private const int UserId = 1;
        private const int BookId = 2;
        private const int LibraryId = 3;
        private const int BookCopyId = 10;

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task GetUserLoansAsync_UserHasLoans_ReturnsSuccessWithLoansMappedToDtos()
        {
            // Arrange
            var loan = CreateLoan(7, LoanStatus.Issued, DateTime.UtcNow.AddDays(10));
            SetupUserLoans([loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            var dto = Assert.Single(result.Value);
            Assert.Equal(7, dto.LoanId);
            Assert.Equal("Кобзар", dto.BookTitle);
            Assert.Equal("Тарас Шевченко", dto.BookAuthor);
            Assert.Equal("Центральна бібліотека", dto.LibraryName);
            Assert.Equal(15, dto.InventoryNumber);
            Assert.Equal(LoanStatus.Issued, dto.Status);
        }

        [Fact]
        public async Task GetUserLoansAsync_UserHasNoLoans_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupUserLoans([]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetUserLoansAsync_IssuedLoan_ReturnsIssueAndDueDates()
        {
            // Arrange
            var loan = CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(10));
            SetupUserLoans([loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            var dto = Assert.Single(result.Value);
            Assert.Equal(loan.IssueDate, dto.IssueDate);
            Assert.Equal(loan.DueDate, dto.DueDate);
        }

        [Fact]
        public async Task GetUserLoansAsync_RequestedLoan_ReturnsNoDates()
        {
            // Arrange
            SetupUserLoans([CreateLoan(1, LoanStatus.Requested, DateTime.UtcNow)]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            var dto = Assert.Single(result.Value);
            Assert.Null(dto.IssueDate);
            Assert.Null(dto.DueDate);
        }

        [Fact]
        public async Task GetUserLoansAsync_ReturnedLoan_ReturnsReturnDate()
        {
            // Arrange
            var loan = CreateLoan(1, LoanStatus.Returned, DateTime.UtcNow.AddDays(-5));
            loan.ReturnDate = DateTime.UtcNow.AddDays(-6);
            SetupUserLoans([loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.Equal(loan.ReturnDate, Assert.Single(result.Value).ReturnDate);
        }

        [Fact]
        public async Task GetUserLoansAsync_IssuedLoanPastDueDate_IsOverdue()
        {
            // Arrange
            SetupUserLoans([CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(-1))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.True(Assert.Single(result.Value).IsOverdue);
        }

        [Fact]
        public async Task GetUserLoansAsync_IssuedLoanBeforeDueDate_IsNotOverdue()
        {
            // Arrange
            SetupUserLoans([CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(3))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.False(Assert.Single(result.Value).IsOverdue);
        }

        [Fact]
        public async Task GetUserLoansAsync_ReturnedLoanPastDueDate_IsNotOverdue()
        {
            // Arrange
            SetupUserLoans([CreateLoan(1, LoanStatus.Returned, DateTime.UtcNow.AddDays(-10))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.False(Assert.Single(result.Value).IsOverdue);
        }

        [Fact]
        public async Task GetUserLoansAsync_StatusFilter_PassesStatusToRepository()
        {
            // Arrange
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, LoanStatus.Issued, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Loan>());
            var sut = CreateService();

            // Act
            await sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = UserId, Status = LoanStatus.Issued });

            // Assert
            _loanRepository.Verify(
                r => r.GetByUserIdAsync(UserId, LoanStatus.Issued, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task GetUserLoansAsync_ValidQuery_LogsInformation()
        {
            // Arrange
            SetupUserLoans([]);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task GetUserLoansAsync_NullQuery_ThrowsArgumentNullException()
        {
            // Arrange
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetUserLoansAsync_InvalidUserId_ReturnsFailure(int userId)
        {
            // Arrange
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = userId });

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetUserLoansAsync_UndefinedStatus_ReturnsFailure()
        {
            // Arrange
            var query = new GetUserLoansQuery { UserId = UserId, Status = (LoanStatus)999 };
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetUserLoansAsync_InvalidQuery_DoesNotCallRepositories()
        {
            // Arrange
            var sut = CreateService();

            // Act
            await sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = 0 });

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _loanRepository.Verify(
                r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task GetUserLoansAsync_InvalidQuery_LogsWarning()
        {
            // Arrange
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = 0 });

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task GetUserLoansAsync_UserNotFound_ReturnsFailureAndDoesNotQueryLoans()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _loanRepository.Verify(
                r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task GetUserLoansAsync_UserNotFound_LogsWarning()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync((User?)null);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task GetUserLoansAsync_RepositoryFails_ReturnsFailureWithoutTechnicalDetails()
        {
            // Arrange
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database is unavailable."));
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.DoesNotContain("Database is unavailable.", result.Error);
        }

        [Fact]
        public async Task GetUserLoansAsync_RepositoryFails_LogsErrorWithException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task GetUserLoansAsync_RequestCanceled_ThrowsOperationCanceledAndDoesNotLogError()
        {
            // Arrange
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(CreateValidQuery()));

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

        private void SetupUserFound()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(UserId))
                .ReturnsAsync(new User { Id = UserId, Role = UserRole.Reader });
        }

        // Користувач існує, репозиторій повертає вказані позики (без фільтра за статусом)
        private void SetupUserLoans(IReadOnlyList<Loan> loans)
        {
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(loans);
        }

        private static GetUserLoansQuery CreateValidQuery()
        {
            return new GetUserLoansQuery { UserId = UserId };
        }

        private static Loan CreateLoan(int id, LoanStatus status, DateTime dueDate)
        {
            return new Loan
            {
                Id = id,
                UserId = UserId,
                BookCopyId = BookCopyId,
                IssueDate = dueDate.AddDays(-14),
                DueDate = dueDate,
                Status = status,
                BookCopy = new BookCopy
                {
                    Id = BookCopyId,
                    InventoryNumber = 15,
                    Book = new Book { Id = BookId, Title = "Кобзар", Author = "Тарас Шевченко" },
                    Library = new Library { Id = LibraryId, Name = "Центральна бібліотека" }
                }
            };
        }
    }
}
