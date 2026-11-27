using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Libraries.Services
{
    // Unit-тести юз кейсу «Редагувати інформацію про філію» (LibraryService.UpdateLibraryAsync)
    public class LibraryServiceUpdateLibraryTests
    {
        private const int AdminId = 1;
        private const int LibraryId = 12;
        private const string StoredName = "Центральна бібліотека";
        private const string StoredAddress = "м. Львів, вул. Стрийська, 10";
        private const string StoredContactInfo = "+380321234567, central@bibliogo.com";
        private const string NewAddress = "м. Львів, вул. Городоцька, 25";
        private const string NewContactInfo = "+380327654321, new@bibliogo.com";
        private const char FillerCharacter = 'а';

        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly LibraryService _service;

        public LibraryServiceUpdateLibraryTests()
        {
            _service = new LibraryService(
                _libraryRepository.Object,
                _userRepository.Object,
                NullLogger<LibraryService>.Instance);
        }

        [Fact]
        public async Task UpdateLibraryAsync_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            SetupAdminAndLibrary();

            // Act
            var result = await _service.UpdateLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UpdateLibraryAsync_ValidCommand_SavesNewAddressAndContactInfoAndKeepsName()
        {
            // Arrange
            SetupAdminAndLibrary();

            // Act
            await _service.UpdateLibraryAsync(CreateCommand());

            // Assert
            _libraryRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<Library>(library => library.Id == LibraryId
                        && library.Name == StoredName
                        && library.Address == NewAddress
                        && library.ContactInfo == NewContactInfo),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateLibraryAsync_FieldsWithSurroundingWhitespace_SavesTrimmedValues()
        {
            // Arrange
            SetupAdminAndLibrary();
            var command = CreateCommand(
                address: "\tм. Львів, вул. Городоцька, 25  ",
                contactInfo: " +380327654321, new@bibliogo.com\r\n");

            // Act
            var result = await _service.UpdateLibraryAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
            _libraryRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<Library>(library => library.Address == NewAddress
                        && library.ContactInfo == NewContactInfo),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateLibraryAsync_FieldsOnMaximumLength_ReturnsSuccess()
        {
            // Arrange
            SetupAdminAndLibrary();
            var command = CreateCommand(
                address: new string(FillerCharacter, 200),
                contactInfo: new string(FillerCharacter, 200));

            // Act
            var result = await _service.UpdateLibraryAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Librarian)]
        public async Task UpdateLibraryAsync_ActorIsNotAdministrator_ReturnsFailureAndDoesNotSave(UserRole actorRole)
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(AdminId))
                .ReturnsAsync(new User { Id = AdminId, Role = actorRole });

            // Act
            var result = await _service.UpdateLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyNothingSaved();
        }

        [Fact]
        public async Task UpdateLibraryAsync_ActorNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(AdminId)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyNothingSaved();
        }

        [Fact]
        public async Task UpdateLibraryAsync_LibraryNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            SetupAdmin();
            _libraryRepository
                .Setup(r => r.GetByIdAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Library?)null);

            // Act
            var result = await _service.UpdateLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _libraryRepository.Verify(r => r.UpdateAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, LibraryId, NewAddress, NewContactInfo)]
        [InlineData(-5, LibraryId, NewAddress, NewContactInfo)]
        [InlineData(AdminId, 0, NewAddress, NewContactInfo)]
        [InlineData(AdminId, -3, NewAddress, NewContactInfo)]
        [InlineData(AdminId, LibraryId, "", NewContactInfo)]
        [InlineData(AdminId, LibraryId, "   ", NewContactInfo)]
        [InlineData(AdminId, LibraryId, NewAddress, "")]
        [InlineData(AdminId, LibraryId, NewAddress, "   ")]
        public async Task UpdateLibraryAsync_InvalidData_ReturnsFailureAndSkipsRepositories(
            int actorId, int libraryId, string address, string contactInfo)
        {
            // Arrange
            var command = CreateCommand(actorId: actorId, libraryId: libraryId, address: address, contactInfo: contactInfo);

            // Act
            var result = await _service.UpdateLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        [Fact]
        public async Task UpdateLibraryAsync_AddressTooLong_ReturnsFailureAndSkipsRepositories()
        {
            // Arrange
            var command = CreateCommand(address: new string(FillerCharacter, 201));

            // Act
            var result = await _service.UpdateLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        [Fact]
        public async Task UpdateLibraryAsync_ContactInfoTooLong_ReturnsFailureAndSkipsRepositories()
        {
            // Arrange
            var command = CreateCommand(contactInfo: new string(FillerCharacter, 201));

            // Act
            var result = await _service.UpdateLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        private static UpdateLibraryCommand CreateCommand(
            int actorId = AdminId,
            int libraryId = LibraryId,
            string address = NewAddress,
            string contactInfo = NewContactInfo) => new()
        {
            ActorId = actorId,
            LibraryId = libraryId,
            Address = address,
            ContactInfo = contactInfo
        };

        private void SetupAdmin()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(AdminId))
                .ReturnsAsync(new User { Id = AdminId, Email = "admin@mail.com", Role = UserRole.Administrator });
        }

        private void SetupAdminAndLibrary()
        {
            SetupAdmin();
            _libraryRepository
                .Setup(r => r.GetByIdAsync(LibraryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Library
                {
                    Id = LibraryId,
                    Name = StoredName,
                    Address = StoredAddress,
                    ContactInfo = StoredContactInfo
                });
            _libraryRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void VerifyNothingSaved()
        {
            _libraryRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _libraryRepository.Verify(r => r.UpdateAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()), Times.Never);
        }
         [Fact]
        public async Task GetLibrariesAsync_LibrariesExist_ReturnsSuccessWithAllFieldsMapped()
        {
            // Arrange
            SetupLibraries(
                new Library { Id = 1, Name = "Центральна", Address = "вул. Стрийська, 10", ContactInfo = "+380321234567" });

            // Act
            var result = await _service.GetLibrariesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            var item = Assert.Single(result.Value);
            Assert.Equal(1, item.Id);
            Assert.Equal("Центральна", item.Name);
            Assert.Equal("вул. Стрийська, 10", item.Address);
            Assert.Equal("+380321234567", item.ContactInfo);
        }

        [Fact]
        public async Task GetLibrariesAsync_SeveralLibraries_KeepsRepositoryOrderAndCount()
        {
            // Arrange
            SetupLibraries(
                new Library { Id = 2, Name = "Академічна", Address = "а", ContactInfo = "к" },
                new Library { Id = 1, Name = "Центральна", Address = "б", ContactInfo = "л" },
                new Library { Id = 3, Name = "Юнацька", Address = "в", ContactInfo = "м" });

            // Act
            var result = await _service.GetLibrariesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { 2, 1, 3 }, result.Value.Select(item => item.Id));
        }

        [Fact]
        public async Task GetLibrariesAsync_NoLibraries_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupLibraries();

            // Act
            var result = await _service.GetLibrariesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        private void SetupLibraries(params Library[] libraries)
        {
            IReadOnlyList<Library> stored = libraries.ToList();
            _libraryRepository
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored);
        }
    }
}
