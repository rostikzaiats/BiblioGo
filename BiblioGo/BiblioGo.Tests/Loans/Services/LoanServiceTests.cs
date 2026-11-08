using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Commands;
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
    // Unit-тести юз кейсів «Оформити заявку на отримання книги» (LoanService.CreateRequestAsync)
    // та «Переглянути власні позики та терміни» (LoanService.GetUserLoansAsync)
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

        // ---------- Use case 2: переглянути власні позики та терміни ----------

        [Fact]
        public async Task GetUserLoansAsync_UserHasLoans_ReturnsLoansMappedToDtos()
        {
            // Arrange
            var loan = CreateLoan(7, LoanStatus.Issued, DateTime.UtcNow.AddDays(10));
            SetupUserLoans([loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal(7, dto.LoanId);
            Assert.Equal("Кобзар", dto.BookTitle);
            Assert.Equal("Тарас Шевченко", dto.BookAuthor);
            Assert.Equal("Центральна бібліотека", dto.LibraryName);
            Assert.Equal(15, dto.InventoryNumber);
            Assert.Equal(LoanStatus.Issued, dto.Status);
        }

        [Fact]
        public async Task GetUserLoansAsync_UserHasNoLoans_ReturnsEmptyList()
        {
            // Arrange
            SetupUserLoans([]);
            var sut = CreateService();

            // Act
            var result = await sut.GetUserLoansAsync(CreateValidQuery());

            // Assert
            Assert.Empty(result);
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
            var dto = Assert.Single(result);
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
            var dto = Assert.Single(result);
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
            Assert.Equal(loan.ReturnDate, Assert.Single(result).ReturnDate);
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
            Assert.True(Assert.Single(result).IsOverdue);
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
            Assert.False(Assert.Single(result).IsOverdue);
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
            Assert.False(Assert.Single(result).IsOverdue);
        }

        [Fact]
        public async Task GetUserLoansAsync_StatusFilter_PassesStatusToRepository()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync(new User { Id = UserId, Role = UserRole.Reader });
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
        public async Task GetUserLoansAsync_InvalidUserId_ThrowsInputValidationException(int userId)
        {
            // Arrange
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = userId }));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task GetUserLoansAsync_UndefinedStatus_ThrowsInputValidationException()
        {
            // Arrange
            var query = new GetUserLoansQuery { UserId = UserId, Status = (LoanStatus)999 };
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(query));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task GetUserLoansAsync_InvalidQuery_DoesNotCallRepositories()
        {
            // Arrange
            var sut = CreateService();

            // Act
            await Record.ExceptionAsync(() => sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = 0 }));

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
            await Record.ExceptionAsync(() => sut.GetUserLoansAsync(new GetUserLoansQuery { UserId = 0 }));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task GetUserLoansAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(UserId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(CreateValidQuery()));

            // Assert
            Assert.IsType<NotFoundException>(exception);
            _loanRepository.Verify(
                r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task GetUserLoansAsync_RepositoryFails_RethrowsRepositoryException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var sut = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => sut.GetUserLoansAsync(CreateValidQuery()));

            // Assert
            Assert.Same(repositoryException, exception);
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
            await Record.ExceptionAsync(() => sut.GetUserLoansAsync(CreateValidQuery()));

            // Assert
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task GetUserLoansAsync_RequestCanceled_DoesNotLogError()
        {
            // Arrange
            SetupUserFound();
            _loanRepository
                .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<LoanStatus?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await Record.ExceptionAsync(() => sut.GetUserLoansAsync(CreateValidQuery()));

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
