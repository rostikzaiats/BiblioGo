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
    // Unit-тести юз кейсу «Змінити статус примірника» (LoanService.ChangeBookCopyStatusAsync)
    public class LoanServiceChangeCopyStatusTests
    {
        private const int InventoryNumber = 1001;
        private const int LibrarianId = 2;
        private const int BookCopyId = 10;
        private const string BookTitle = "Кобзар";
        private const string LibraryName = "Центральна філія";

        private readonly Mock<ILoanRepository> _loanRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();

        [Fact]
        public async Task ChangeBookCopyStatusAsync_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_ValidCommand_ReturnsChangeDetails()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsSuccess);
            var change = result.Value;
            Assert.Equal(BookCopyId, change.BookCopyId);
            Assert.Equal(InventoryNumber, change.InventoryNumber);
            Assert.Equal(BookTitle, change.BookTitle);
            Assert.Equal(LibraryName, change.LibraryName);
            Assert.Equal(BookCopyStatus.Available, change.PreviousStatus);
            Assert.Equal(BookCopyStatus.WrittenOff, change.NewStatus);
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_ValidCommand_ChangesStatusOnlyFromCurrentOne()
        {
            // Arrange
            SetupHappyPath();
            var sut = CreateService();

            // Act
            await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            _bookCopyRepository.Verify(
                r => r.ChangeStatusAsync(BookCopyId, BookCopyStatus.Available, BookCopyStatus.WrittenOff, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_WrittenOffCopyToAvailable_ReturnsSuccess()
        {
            // Arrange
            SetupHappyPath(BookCopyStatus.WrittenOff, BookCopyStatus.Available);
            var command = CreateValidCommand(BookCopyStatus.Available);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_ValidCommand_LogsStatusChanged()
        {
            // Arrange
            SetupHappyPath();
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        }

        [Theory]
        [InlineData(0, LibrarianId)]
        [InlineData(-1, LibrarianId)]
        [InlineData(InventoryNumber, 0)]
        [InlineData(InventoryNumber, -1)]
        public async Task ChangeBookCopyStatusAsync_InvalidIds_ReturnsFailure(int inventoryNumber, int librarianId)
        {
            // Arrange
            var command = new ChangeBookCopyStatusCommand
            {
                InventoryNumber = inventoryNumber,
                LibrarianId = librarianId,
                NewStatus = BookCopyStatus.WrittenOff
            };
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_UndefinedStatus_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand((BookCopyStatus)99);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_InvalidCommand_DoesNotCallRepositories()
        {
            // Arrange
            var command = CreateValidCommand();
            command.InventoryNumber = 0;
            var sut = CreateService();

            // Act
            await sut.ChangeBookCopyStatusAsync(command);

            // Assert
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _bookCopyRepository.Verify(
                r => r.FindByInventoryNumberAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_InvalidCommand_LogsWarning()
        {
            // Arrange
            var command = CreateValidCommand();
            command.InventoryNumber = 0;
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ChangeBookCopyStatusAsync(command);

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_LibrarianNotFound_ReturnsFailureAndDoesNotChangeStatus()
        {
            // Arrange
            SetupHappyPath();
            _userRepository.Setup(r => r.FindByIdAsync(LibrarianId)).ReturnsAsync((User?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            VerifyStatusNotChanged();
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Administrator)]
        public async Task ChangeBookCopyStatusAsync_UserIsNotLibrarian_ReturnsFailureAndDoesNotChangeStatus(UserRole role)
        {
            // Arrange
            SetupHappyPath();
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = role });
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyStatusNotChanged();
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_CopyNotFound_ReturnsFailureAndDoesNotChangeStatus()
        {
            // Arrange
            SetupHappyPath();
            _bookCopyRepository
                .Setup(r => r.FindByInventoryNumberAsync(InventoryNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BookCopy?)null);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyStatusNotChanged();
        }

        [Theory]
        [InlineData(BookCopyStatus.Available)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task ChangeBookCopyStatusAsync_SameStatus_ReturnsFailureAndDoesNotChangeStatus(BookCopyStatus status)
        {
            // Arrange
            SetupHappyPath(status, status);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand(status));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyStatusNotChanged();
        }

        [Theory]
        [InlineData(BookCopyStatus.Available)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task ChangeBookCopyStatusAsync_NewStatusIsIssued_ReturnsFailureAndDoesNotChangeStatus(BookCopyStatus currentStatus)
        {
            // Arrange
            SetupHappyPath(currentStatus, BookCopyStatus.Issued);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand(BookCopyStatus.Issued));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyStatusNotChanged();
        }

        [Theory]
        [InlineData(BookCopyStatus.Available)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task ChangeBookCopyStatusAsync_CopyIsIssued_ReturnsFailureAndDoesNotChangeStatus(BookCopyStatus newStatus)
        {
            // Arrange
            SetupHappyPath(BookCopyStatus.Issued, newStatus);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand(newStatus));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyStatusNotChanged();
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_StateChangedConcurrently_ReturnsFailure()
        {
            // Arrange
            SetupHappyPath();
            SetupChangeResult(BookCopyStatus.Available, BookCopyStatus.WrittenOff, false);
            var sut = CreateService();

            // Act
            var result = await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_StateChangedConcurrently_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath();
            SetupChangeResult(BookCopyStatus.Available, BookCopyStatus.WrittenOff, false);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task ChangeBookCopyStatusAsync_BusinessRuleViolation_LogsWarningNotError()
        {
            // Arrange
            SetupHappyPath(BookCopyStatus.Issued, BookCopyStatus.WrittenOff);
            var logger = new FakeLogger<LoanService>();
            var sut = CreateService(logger);

            // Act
            await sut.ChangeBookCopyStatusAsync(CreateValidCommand());

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

        // Бібліотекар існує, примірник знайдено з currentStatus, зміна на newStatus проходить успішно
        private void SetupHappyPath(
            BookCopyStatus currentStatus = BookCopyStatus.Available,
            BookCopyStatus newStatus = BookCopyStatus.WrittenOff)
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(LibrarianId))
                .ReturnsAsync(new User { Id = LibrarianId, Role = UserRole.Librarian });
            _bookCopyRepository
                .Setup(r => r.FindByInventoryNumberAsync(InventoryNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BookCopy
                {
                    Id = BookCopyId,
                    InventoryNumber = InventoryNumber,
                    Status = currentStatus,
                    Book = new Book { Title = BookTitle },
                    Library = new Library { Name = LibraryName }
                });
            SetupChangeResult(currentStatus, newStatus, true);
        }

        private void SetupChangeResult(BookCopyStatus currentStatus, BookCopyStatus newStatus, bool changed)
        {
            _bookCopyRepository
                .Setup(r => r.ChangeStatusAsync(BookCopyId, currentStatus, newStatus, It.IsAny<CancellationToken>()))
                .ReturnsAsync(changed);
        }

        private void VerifyStatusNotChanged()
        {
            _bookCopyRepository.Verify(
                r => r.ChangeStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static ChangeBookCopyStatusCommand CreateValidCommand(BookCopyStatus newStatus = BookCopyStatus.WrittenOff)
        {
            return new ChangeBookCopyStatusCommand
            {
                InventoryNumber = InventoryNumber,
                LibrarianId = LibrarianId,
                NewStatus = newStatus
            };
        }
    }
}
