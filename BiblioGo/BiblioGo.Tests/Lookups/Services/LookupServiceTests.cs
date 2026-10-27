using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Lookups.Services;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Lookups.Services
{
    // Unit-тести довідкових списків для випадайок (LookupService)
    public class LookupServiceTests
    {
        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly Mock<IBookCopyRepository> _bookCopyRepository = new();
        private readonly LookupService _sut;

        public LookupServiceTests()
        {
            _sut = new LookupService(
                _bookRepository.Object,
                _libraryRepository.Object,
                _bookCopyRepository.Object,
                NullLogger<LookupService>.Instance);
        }

        [Fact]
        public async Task GetBooksAsync_BooksExist_ReturnsItemsWithTitleAndAuthor()
        {
            // Arrange
            IReadOnlyList<Book> books =
            [
                new Book { Id = 1, Title = "Кобзар", Author = "Тарас Шевченко" },
                new Book { Id = 2, Title = "Intermezzo", Author = "Михайло Коцюбинський" }
            ];
            _bookRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(books);

            // Act
            var result = await _sut.GetBooksAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(1, result.Value[0].Id);
            Assert.Equal("Кобзар — Тарас Шевченко", result.Value[0].Name);
            Assert.Equal(2, result.Value[1].Id);
        }

        [Fact]
        public async Task GetBooksAsync_NoBooks_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            IReadOnlyList<Book> noBooks = [];
            _bookRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(noBooks);

            // Act
            var result = await _sut.GetBooksAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetBooksAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            _bookRepository
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.GetBooksAsync();

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetLibrariesAsync_LibrariesExist_ReturnsItemsWithLibraryName()
        {
            // Arrange
            IReadOnlyList<Library> libraries =
            [
                new Library { Id = 3, Name = "Центральна бібліотека" },
                new Library { Id = 4, Name = "Філія на Сихові" }
            ];
            _libraryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(libraries);

            // Act
            var result = await _sut.GetLibrariesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(3, result.Value[0].Id);
            Assert.Equal("Центральна бібліотека", result.Value[0].Name);
        }

        [Fact]
        public async Task GetLibrariesAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            _libraryRepository
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.GetLibrariesAsync();

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task GetAvailableCopiesAsync_CopiesExist_ReturnsItemsWithInventoryNumberAndLibrary()
        {
            // Arrange
            IReadOnlyList<BookCopy> copies =
            [
                new BookCopy { Id = 10, InventoryNumber = 101, Library = new Library { Id = 3, Name = "Центральна бібліотека" } },
                new BookCopy { Id = 11, InventoryNumber = 102, Library = new Library { Id = 4, Name = "Філія на Сихові" } }
            ];
            _bookCopyRepository
                .Setup(r => r.GetAvailableByBookAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(copies);

            // Act
            var result = await _sut.GetAvailableCopiesAsync(5);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(10, result.Value[0].Id);
            Assert.Equal("№ 101 — Центральна бібліотека", result.Value[0].Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetAvailableCopiesAsync_InvalidBookId_ReturnsFailureAndSkipsRepository(int bookId)
        {
            // Act
            var result = await _sut.GetAvailableCopiesAsync(bookId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _bookCopyRepository.Verify(
                r => r.GetAvailableByBookAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task GetAvailableCopiesAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            _bookCopyRepository
                .Setup(r => r.GetAvailableByBookAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.GetAvailableCopiesAsync(5);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }
    }
}
