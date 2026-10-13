using BiblioGo.Application.Books.Commands;
using BiblioGo.Application.Books.Services;
using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace BiblioGo.Tests.Books.Services
{
    // Unit-тести юз кейсу «Списати примірник» (BookCopyService.WriteOffAsync)
    public class BookCopyServiceTests
    {
        private const int ExistingCopyId = 7;

        [Fact]
        public async Task WriteOffAsync_AvailableCopy_SetsWrittenOffStatus()
        {
            // Arrange
            var mockRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Available));
            var service = CreateService(mockRepository.Object);

            // Act
            await service.WriteOffAsync(CreateCommand());

            // Assert
            mockRepository.Verify(
                repository => repository.SetStatusAsync(ExistingCopyId, BookCopyStatus.WrittenOff, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task WriteOffAsync_IssuedCopy_ThrowsBusinessRuleException()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Issued));
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.IsType<BusinessRuleException>(exception);
        }

        [Fact]
        public async Task WriteOffAsync_AlreadyWrittenOffCopy_ThrowsBusinessRuleException()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.WrittenOff));
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.IsType<BusinessRuleException>(exception);
        }

        [Theory]
        [InlineData(BookCopyStatus.Issued)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task WriteOffAsync_CopyCannotBeWrittenOff_DoesNotChangeStatus(BookCopyStatus status)
        {
            // Arrange
            var mockRepository = CreateRepositoryReturning(CreateCopy(status));
            var service = CreateService(mockRepository.Object);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            VerifyStatusNeverChanged(mockRepository);
        }

        [Fact]
        public async Task WriteOffAsync_CopyNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var stubRepository = CreateRepositoryReturning(null);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.IsType<NotFoundException>(exception);
        }

        [Fact]
        public async Task WriteOffAsync_CopyNotFound_DoesNotChangeStatus()
        {
            // Arrange
            var mockRepository = CreateRepositoryReturning(null);
            var service = CreateService(mockRepository.Object);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            VerifyStatusNeverChanged(mockRepository);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task WriteOffAsync_InvalidCopyId_ThrowsInputValidationException(int bookCopyId)
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookCopyRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand(bookCopyId)));

            // Assert
            Assert.IsType<InputValidationException>(exception);
        }

        [Fact]
        public async Task WriteOffAsync_InvalidCopyId_DoesNotQueryRepository()
        {
            // Arrange
            var mockRepository = new Mock<IBookCopyRepository>();
            var service = CreateService(mockRepository.Object);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand(0)));

            // Assert
            mockRepository.Verify(
                repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
            VerifyStatusNeverChanged(mockRepository);
        }

        [Fact]
        public async Task WriteOffAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService(Mock.Of<IBookCopyRepository>());

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task WriteOffAsync_CopyWrittenOff_LogsInformationWithCopyId()
        {
            // Arrange
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Available));
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await service.WriteOffAsync(CreateCommand());

            // Assert
            Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
            Assert.Equal("7", logger.LatestRecord.GetStructuredStateValue("BookCopyId"));
        }

        [Fact]
        public async Task WriteOffAsync_InvalidCopyId_LogsWarning()
        {
            // Arrange
            var logger = new FakeLogger<BookCopyService>();
            var service = CreateService(Mock.Of<IBookCopyRepository>(), logger);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand(0)));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        }

        [Theory]
        [InlineData(BookCopyStatus.Issued)]
        [InlineData(BookCopyStatus.WrittenOff)]
        public async Task WriteOffAsync_CopyCannotBeWrittenOff_LogsWarningNotError(BookCopyStatus status)
        {
            // Arrange
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = CreateRepositoryReturning(CreateCopy(status));
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task WriteOffAsync_CopyNotFound_LogsWarningNotError()
        {
            // Arrange
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = CreateRepositoryReturning(null);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task WriteOffAsync_GetCopyFails_RethrowsRepositoryException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var stubRepository = new Mock<IBookCopyRepository>();
            stubRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var service = CreateService(stubRepository.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Same(repositoryException, exception);
        }

        [Fact]
        public async Task WriteOffAsync_SetStatusFails_RethrowsAndLogsErrorWithException()
        {
            // Arrange
            var repositoryException = new InvalidOperationException("Database is unavailable.");
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Available));
            stubRepository
                .Setup(r => r.SetStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(repositoryException);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Same(repositoryException, exception);
            Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
            Assert.Same(repositoryException, logger.LatestRecord.Exception);
        }

        [Fact]
        public async Task WriteOffAsync_CopyDeletedBeforeStatusChange_RethrowsNotFoundWithoutErrorLog()
        {
            // Arrange
            var notFoundException = new NotFoundException("Примірник не знайдено.");
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Available));
            stubRepository
                .Setup(r => r.SetStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(notFoundException);
            var service = CreateService(stubRepository.Object, logger);

            // Act
            var exception = await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.Same(notFoundException, exception);
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task WriteOffAsync_OperationCanceled_DoesNotLogError()
        {
            // Arrange
            var logger = new FakeLogger<BookCopyService>();
            var stubRepository = new Mock<IBookCopyRepository>();
            stubRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var service = CreateService(stubRepository.Object, logger);

            // Act
            await Record.ExceptionAsync(() => service.WriteOffAsync(CreateCommand()));

            // Assert
            Assert.DoesNotContain(logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        }

        [Fact]
        public async Task WriteOffAsync_CancellationTokenProvided_PassesItToRepository()
        {
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var mockRepository = CreateRepositoryReturning(CreateCopy(BookCopyStatus.Available));
            var service = CreateService(mockRepository.Object);

            // Act
            await service.WriteOffAsync(CreateCommand(), token);

            // Assert
            mockRepository.Verify(repository => repository.GetByIdAsync(ExistingCopyId, token), Times.Once);
            mockRepository.Verify(
                repository => repository.SetStatusAsync(ExistingCopyId, BookCopyStatus.WrittenOff, token),
                Times.Once);
        }

        private static void VerifyStatusNeverChanged(Mock<IBookCopyRepository> mockRepository)
        {
            mockRepository.Verify(
                repository => repository.SetStatusAsync(It.IsAny<int>(), It.IsAny<BookCopyStatus>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static BookCopyService CreateService(IBookCopyRepository repository, ILogger<BookCopyService>? logger = null)
        {
            return new BookCopyService(repository, logger ?? NullLogger<BookCopyService>.Instance);
        }

        private static Mock<IBookCopyRepository> CreateRepositoryReturning(BookCopy? bookCopy)
        {
            var repository = new Mock<IBookCopyRepository>();
            repository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bookCopy);

            return repository;
        }

        private static WriteOffBookCopyCommand CreateCommand(int bookCopyId = ExistingCopyId)
        {
            return new WriteOffBookCopyCommand { BookCopyId = bookCopyId };
        }

        private static BookCopy CreateCopy(BookCopyStatus status)
        {
            return new BookCopy { Id = ExistingCopyId, BookId = 1, InventoryNumber = 1001, LibraryId = 1, Status = status };
        }
    }
}
