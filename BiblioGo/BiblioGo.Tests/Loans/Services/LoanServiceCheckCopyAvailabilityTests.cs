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
    // Unit-тести юз кейсу «Перевірити наявність примірника перед видачею» (LoanService.CheckCopyAvailabilityAsync)
    public class LoanServiceCheckCopyAvailabilityTests
    {
        private const int LoanId = 5;
        private const int LibrarianId = 2;
        private const int BookCopyId = 10;
        private const int InventoryNumber = 1007;
        private const string BookTitle = "Кобзар";
        private const string BookAuthor = "Тарас Шевченко";
        private const string LibraryName = "Центральна бібліотека";

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task CheckCopyAvailabilityAsync_CopyOnShelf_ReturnsSuccessWithAvailableCopy()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Requested, BookCopyStatus.Available);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsAvailable);
            Assert.Empty(result.Value.UnavailableReason);
            Assert.Equal(BookCopyStatus.Available, result.Value.CopyStatus);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_CopyOnShelf_ReturnsCopyDetails()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Requested, BookCopyStatus.Available);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(LoanId, result.Value.LoanId);
            Assert.Equal(BookCopyId, result.Value.BookCopyId);
            Assert.Equal(InventoryNumber, result.Value.InventoryNumber);
            Assert.Equal(BookTitle, result.Value.BookTitle);
            Assert.Equal(BookAuthor, result.Value.BookAuthor);
            Assert.Equal(LibraryName, result.Value.LibraryName);
        }

        [Theory]
        [InlineData(BookCopyStatus.Issued, "Примірник уже виданий іншому читачу")]
        [InlineData(BookCopyStatus.WrittenOff, "Примірник списано, видати його не можна")]
        public async Task CheckCopyAvailabilityAsync_CopyNotOnShelf_ReturnsSuccessWithReason(BookCopyStatus copyStatus, string expectedReason)
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Requested, copyStatus);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.IsAvailable);
            Assert.Equal(copyStatus, result.Value.CopyStatus);
            Assert.Equal(expectedReason, result.Value.UnavailableReason);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_ValidQuery_DoesNotChangeLoanOrCopy()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Requested, BookCopyStatus.Available);
            var sut = CreateService();

            // Act
            await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            _loanRepository.Verify(
                r => r.ConfirmIssueAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never());
            _bookCopyRepository.Verify(
                r => r.SetStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_ValidQuery_WritesNoLogs()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Requested, BookCopyStatus.Issued);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.Empty(logger.Collector.GetSnapshot());
        }

        [Theory]
        [InlineData(0, LibrarianId, "Вкажіть коректний номер заявки")]
        [InlineData(-1, LibrarianId, "Вкажіть коректний номер заявки")]
        [InlineData(LoanId, 0, "Некоректний ідентифікатор бібліотекаря")]
        [InlineData(LoanId, -1, "Некоректний ідентифікатор бібліотекаря")]
        public async Task CheckCopyAvailabilityAsync_InvalidIds_ReturnsFailureWithValidationMessage(int loanId, int librarianId, string expectedError)
        {
            // Arrange
            var sut = CreateService();
            var query = new CheckCopyAvailabilityQuery { LoanId = loanId, LibrarianId = librarianId };

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(query);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_InvalidIds_DoesNotQueryRepositories()
        {
            // Arrange
            var sut = CreateService();

            // Act
            await sut.CheckCopyAvailabilityAsync(new CheckCopyAvailabilityQuery { LoanId = 0, LibrarianId = LibrarianId });

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _loanRepository.Verify(r => r.GetWithCopyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_InvalidIds_LogsOneWarning()
        {
            // Arrange
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.CheckCopyAvailabilityAsync(new CheckCopyAvailabilityQuery { LoanId = 0, LibrarianId = LibrarianId });

            // Assert
            var record = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Warning, record.Level);
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Administrator)]
        public async Task CheckCopyAvailabilityAsync_UserIsNotLibrarian_ReturnsFailureAndDoesNotLoadLoan(UserRole role)
        {
            // Arrange
            SetupLibrarian(role);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Цю дію може виконувати лише бібліотекар", result.Error);
            _loanRepository.Verify(r => r.GetWithCopyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_LibrarianNotFound_ReturnsFailure()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(LibrarianId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Бібліотекаря не знайдено", result.Error);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_LoanNotFound_ReturnsFailureWithNotFoundMessage()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            _loanRepository
                .Setup(r => r.GetWithCopyAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Loan?)null);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Заявку не знайдено", result.Error);
        }

        [Theory]
        [InlineData(LoanStatus.Issued, "Книгу за цією заявкою вже видано")]
        [InlineData(LoanStatus.Returned, "Заявку вже закрито: книгу повернено")]
        public async Task CheckCopyAvailabilityAsync_LoanNotAwaitingIssue_ReturnsFailure(LoanStatus loanStatus, string expectedError)
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(loanStatus, BookCopyStatus.Issued);
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_LoanNotAwaitingIssue_LogsWarningNotError()
        {
            // Arrange
            SetupLibrarian(UserRole.Librarian);
            SetupLoan(LoanStatus.Issued, BookCopyStatus.Issued);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.CheckCopyAvailabilityAsync(CreateValidQuery());

            // Assert
            var record = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Warning, record.Level);
        }

        [Fact]
        public async Task CheckCopyAvailabilityAsync_CancellationToken_PassedToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            SetupLibrarian(UserRole.Librarian);
            _loanRepository
                .Setup(r => r.GetWithCopyAsync(LoanId, token))
                .ReturnsAsync(CreateLoan(LoanStatus.Requested, BookCopyStatus.Available));
            var sut = CreateService();

            // Act
            var result = await sut.CheckCopyAvailabilityAsync(CreateValidQuery(), token);

            // Assert
            Assert.True(result.IsSuccess);
            _loanRepository.Verify(r => r.GetWithCopyAsync(LoanId, token), Times.Once());
        }

        private LoanService CreateService(ILogger<LoanService>? logger = null)
        {
            return new LoanService(
                _loanRepository.Object,
                _bookCopyRepository.Object,
                _userRepository.Object,
                logger ?? NullLogger<LoanService>.Instance);
        }

        private void SetupLibrarian(UserRole role)
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = role });
        }

        private void SetupLoan(LoanStatus loanStatus, BookCopyStatus copyStatus)
        {
            _loanRepository
                .Setup(r => r.GetWithCopyAsync(LoanId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateLoan(loanStatus, copyStatus));
        }

        private static Loan CreateLoan(LoanStatus loanStatus, BookCopyStatus copyStatus)
        {
            return new Loan
            {
                Id = LoanId,
                BookCopyId = BookCopyId,
                Status = loanStatus,
                BookCopy = new BookCopy
                {
                    Id = BookCopyId,
                    InventoryNumber = InventoryNumber,
                    Status = copyStatus,
                    Book = new Book { Title = BookTitle, Author = BookAuthor },
                    Library = new Library { Name = LibraryName }
                }
            };
        }

        private static CheckCopyAvailabilityQuery CreateValidQuery()
        {
            return new CheckCopyAvailabilityQuery
            {
                LoanId = LoanId,
                LibrarianId = LibrarianId
            };
        }
    }
}
