using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Loans.Queries;
using BiblioGo.Application.Loans.Services;
using BiblioGo.Application.Loans.Validators;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace BiblioGo.Tests.Loans.Services
{
    // Unit-тести юз кейсу «Переглянути журнал позик» (LoanService.GetLoanJournalAsync)
    public class LoanServiceJournalTests
    {
        private const int LibrarianId = 2;
        private const int LibraryId = 3;
        private const int BookId = 4;
        private const int BookCopyId = 10;
        private const int ReaderId = 1;

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task GetLoanJournalAsync_ValidQuery_ReturnsSuccessWithLoansMappedToDtos()
        {
            // Arrange
            SetupJournal(totalCount: 1, [CreateLoan(7, LoanStatus.Issued, DateTime.UtcNow.AddDays(10))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            var dto = Assert.Single(result.Value.Loans);
            Assert.Equal(7, dto.LoanId);
            Assert.Equal("Іван Петренко", dto.ReaderName);
            Assert.Equal("Кобзар", dto.BookTitle);
            Assert.Equal(15, dto.InventoryNumber);
            Assert.Equal(LoanStatus.Issued, dto.Status);
        }

        [Fact]
        public async Task GetLoanJournalAsync_ValidQuery_ReturnsPageInfo()
        {
            // Arrange
            SetupJournal(totalCount: 25, [CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(3))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.Equal(1, result.Value.PageNumber);
            Assert.Equal(10, result.Value.PageSize);
            Assert.Equal(25, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetLoanJournalAsync_NoLoans_ReturnsEmptyPageWithZeroPages()
        {
            // Arrange
            SetupJournal(totalCount: 0, []);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Loans);
            Assert.Equal(0, result.Value.TotalPages);
        }

        [Theory]
        [InlineData(1, 10, 1)]
        [InlineData(10, 10, 1)]
        [InlineData(11, 10, 2)]
        [InlineData(25, 10, 3)]
        [InlineData(20, 5, 4)]
        public async Task GetLoanJournalAsync_ValidQuery_RoundsTotalPagesUp(int totalCount, int pageSize, int expectedPages)
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageSize = pageSize;
            SetupJournal(totalCount, [], pageSize: pageSize);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.Equal(expectedPages, result.Value.TotalPages);
        }

        [Fact]
        public async Task GetLoanJournalAsync_ThirdPage_SkipsPreviousPages()
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageNumber = 3;
            SetupJournal(totalCount: 30, [], skip: 20);
            var sut = CreateService();

            // Act
            await sut.GetLoanJournalAsync(query);

            // Assert
            _loanRepository.Verify(
                r => r.GetJournalPageAsync(LibraryId, null, null, null, 20, 10, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task GetLoanJournalAsync_StatusFilter_PassesStatusToRepository()
        {
            // Arrange
            var query = CreateValidQuery();
            query.Status = LoanStatus.Requested;
            SetupLibrarianFound();
            _loanRepository
                .Setup(r => r.CountJournalAsync(LibraryId, LoanStatus.Requested, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _loanRepository
                .Setup(r => r.GetJournalPageAsync(LibraryId, LoanStatus.Requested, null, null, 0, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Loan>());
            var sut = CreateService();

            // Act
            await sut.GetLoanJournalAsync(query);

            // Assert
            _loanRepository.Verify(
                r => r.GetJournalPageAsync(LibraryId, LoanStatus.Requested, null, null, 0, 10, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task GetLoanJournalAsync_DateFilters_PassUtcRangeIncludingWholeLastDay()
        {
            // Arrange
            var dateFrom = DateTime.Today.AddDays(-7);
            var dateTo = DateTime.Today;
            var query = CreateValidQuery();
            query.DateFrom = dateFrom;
            query.DateTo = dateTo;
            var expectedFrom = DateTime.SpecifyKind(dateFrom, DateTimeKind.Local).ToUniversalTime();
            var expectedTo = DateTime.SpecifyKind(dateTo.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            SetupLibrarianFound();
            _loanRepository
                .Setup(r => r.CountJournalAsync(LibraryId, null, expectedFrom, expectedTo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _loanRepository
                .Setup(r => r.GetJournalPageAsync(LibraryId, null, expectedFrom, expectedTo, 0, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Loan>());
            var sut = CreateService();

            // Act
            await sut.GetLoanJournalAsync(query);

            // Assert
            _loanRepository.Verify(
                r => r.GetJournalPageAsync(LibraryId, null, expectedFrom, expectedTo, 0, 10, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task GetLoanJournalAsync_RequestedLoan_ReturnsNoDates()
        {
            // Arrange
            SetupJournal(totalCount: 1, [CreateLoan(1, LoanStatus.Requested, DateTime.UtcNow)]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            var dto = Assert.Single(result.Value.Loans);
            Assert.Null(dto.IssueDate);
            Assert.Null(dto.DueDate);
        }

        [Fact]
        public async Task GetLoanJournalAsync_IssuedLoan_ReturnsIssueAndDueDates()
        {
            // Arrange
            var loan = CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(10));
            SetupJournal(totalCount: 1, [loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            var dto = Assert.Single(result.Value.Loans);
            Assert.Equal(loan.IssueDate, dto.IssueDate);
            Assert.Equal(loan.DueDate, dto.DueDate);
        }

        [Fact]
        public async Task GetLoanJournalAsync_ReturnedLoan_ReturnsReturnDate()
        {
            // Arrange
            var loan = CreateLoan(1, LoanStatus.Returned, DateTime.UtcNow.AddDays(-5));
            loan.ReturnDate = DateTime.UtcNow.AddDays(-6);
            SetupJournal(totalCount: 1, [loan]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.Equal(loan.ReturnDate, Assert.Single(result.Value.Loans).ReturnDate);
        }

        [Fact]
        public async Task GetLoanJournalAsync_IssuedLoanPastDueDate_IsOverdue()
        {
            // Arrange
            SetupJournal(totalCount: 1, [CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(-1))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.True(Assert.Single(result.Value.Loans).IsOverdue);
        }

        [Fact]
        public async Task GetLoanJournalAsync_IssuedLoanBeforeDueDate_IsNotOverdue()
        {
            // Arrange
            SetupJournal(totalCount: 1, [CreateLoan(1, LoanStatus.Issued, DateTime.UtcNow.AddDays(3))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.False(Assert.Single(result.Value.Loans).IsOverdue);
        }

        [Fact]
        public async Task GetLoanJournalAsync_ReturnedLoanPastDueDate_IsNotOverdue()
        {
            // Arrange
            SetupJournal(totalCount: 1, [CreateLoan(1, LoanStatus.Returned, DateTime.UtcNow.AddDays(-10))]);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.False(Assert.Single(result.Value.Loans).IsOverdue);
        }

        [Fact]
        public async Task GetLoanJournalAsync_ValidQuery_LogsInformation()
        {
            // Arrange
            SetupJournal(totalCount: 0, []);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetLoanJournalAsync_InvalidLibrarianId_ReturnsFailure(int librarianId)
        {
            // Arrange
            var query = CreateValidQuery();
            query.LibrarianId = librarianId;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetLoanJournalAsync_InvalidLibraryId_ReturnsFailure(int libraryId)
        {
            // Arrange
            var query = CreateValidQuery();
            query.LibraryId = libraryId;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetLoanJournalAsync_UndefinedStatus_ReturnsFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            query.Status = (LoanStatus)999;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetLoanJournalAsync_DateFromAfterDateTo_ReturnsFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            query.DateFrom = DateTime.Today;
            query.DateTo = DateTime.Today.AddDays(-1);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetLoanJournalAsync_DateToIsMaxDate_ReturnsFailure()
        {
            // Arrange
            var query = CreateValidQuery();
            query.DateTo = DateTime.MaxValue;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(GetLoanJournalValidator.MaxPageNumber + 1)]
        public async Task GetLoanJournalAsync_InvalidPageNumber_ReturnsFailure(int pageNumber)
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageNumber = pageNumber;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(GetLoanJournalValidator.MaxPageSize + 1)]
        public async Task GetLoanJournalAsync_InvalidPageSize_ReturnsFailure(int pageSize)
        {
            // Arrange
            var query = CreateValidQuery();
            query.PageSize = pageSize;
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetLoanJournalAsync_InvalidQuery_DoesNotCallRepositories()
        {
            // Arrange
            var query = CreateValidQuery();
            query.LibraryId = 0;
            var sut = CreateService();

            // Act
            await sut.GetLoanJournalAsync(query);

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            VerifyJournalNotLoaded();
        }

        [Fact]
        public async Task GetLoanJournalAsync_InvalidQuery_LogsWarning()
        {
            // Arrange
            var query = CreateValidQuery();
            query.LibraryId = 0;
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetLoanJournalAsync(query);

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task GetLoanJournalAsync_LibrarianNotFound_ReturnsFailureAndDoesNotLoadJournal()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(LibrarianId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyJournalNotLoaded();
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Administrator)]
        public async Task GetLoanJournalAsync_UserIsNotLibrarian_ReturnsFailureAndDoesNotLoadJournal(UserRole role)
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = role });
            var sut = CreateService();

            // Act
            var result = await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyJournalNotLoaded();
        }

        [Fact]
        public async Task GetLoanJournalAsync_UserIsNotLibrarian_LogsWarning()
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = UserRole.Reader });
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.GetLoanJournalAsync(CreateValidQuery());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        private LoanService CreateService(ILogger<LoanService>? logger = null)
        {
            return new LoanService(
                _loanRepository.Object,
                _bookCopyRepository.Object,
                _userRepository.Object,
                logger ?? NullLogger<LoanService>.Instance);
        }

        private void SetupLibrarianFound()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = UserRole.Librarian });
        }

        // Бібліотекар існує, репозиторій повертає вказану сторінку журналу (без фільтрів за статусом і датою)
        private void SetupJournal(int totalCount, IReadOnlyList<Loan> loans, int skip = 0, int pageSize = 10)
        {
            SetupLibrarianFound();
            _loanRepository
                .Setup(r => r.CountJournalAsync(LibraryId, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(totalCount);
            _loanRepository
                .Setup(r => r.GetJournalPageAsync(LibraryId, null, null, null, skip, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(loans);
        }

        private void VerifyJournalNotLoaded()
        {
            _loanRepository.Verify(
                r => r.CountJournalAsync(It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Never());
            _loanRepository.Verify(
                r => r.GetJournalPageAsync(It.IsAny<int>(), It.IsAny<LoanStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static GetLoanJournalQuery CreateValidQuery()
        {
            return new GetLoanJournalQuery { LibrarianId = LibrarianId, LibraryId = LibraryId };
        }

        private static Loan CreateLoan(int id, LoanStatus status, DateTime dueDate)
        {
            return new Loan
            {
                Id = id,
                UserId = ReaderId,
                BookCopyId = BookCopyId,
                IssueDate = dueDate.AddDays(-14),
                DueDate = dueDate,
                Status = status,
                User = new User { Id = ReaderId, FirstName = "Іван", LastName = "Петренко" },
                BookCopy = new BookCopy
                {
                    Id = BookCopyId,
                    BookId = BookId,
                    LibraryId = LibraryId,
                    InventoryNumber = 15,
                    Book = new Book { Id = BookId, Title = "Кобзар", Author = "Тарас Шевченко" }
                }
            };
        }
    }
}
