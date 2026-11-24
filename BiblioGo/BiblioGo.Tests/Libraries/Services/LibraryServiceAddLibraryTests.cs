using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Libraries.Commands;
using BiblioGo.Application.Libraries.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Libraries.Services
{
    // Unit-тести юз кейсу «Додати нову філію бібліотеки» (LibraryService.AddLibraryAsync)
    public class LibraryServiceAddLibraryTests
    {
        private const int AdminId = 1;
        private const int NewLibraryId = 12;
        private const string ValidName = "Центральна бібліотека";
        private const string ValidAddress = "м. Львів, вул. Стрийська, 10";
        private const string ValidContactInfo = "+380321234567, central@bibliogo.com";
        private const char FillerCharacter = 'а';

        private readonly Mock<ILibraryRepository> _libraryRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly LibraryService _service;

        public LibraryServiceAddLibraryTests()
        {
            _service = new LibraryService(
                _libraryRepository.Object,
                _userRepository.Object,
                NullLogger<LibraryService>.Instance);
        }

        [Fact]
        public async Task AddLibraryAsync_ValidCommand_ReturnsSuccessWithNewLibraryId()
        {
            // Arrange
            SetupAdminAndAdd();

            // Act
            var result = await _service.AddLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewLibraryId, result.Value);
        }

        [Fact]
        public async Task AddLibraryAsync_ValidCommand_SavesLibraryWithCommandData()
        {
            // Arrange
            SetupAdminAndAdd();

            // Act
            await _service.AddLibraryAsync(CreateCommand());

            // Assert
            _libraryRepository.Verify(
                r => r.AddAsync(
                    It.Is<Library>(library => library.Name == ValidName
                        && library.Address == ValidAddress
                        && library.ContactInfo == ValidContactInfo),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddLibraryAsync_FieldsWithSurroundingWhitespace_SavesTrimmedValuesAndChecksTrimmedName()
        {
            // Arrange
            SetupAdminAndAdd();
            var command = CreateCommand(
                name: "  Центральна бібліотека ",
                address: "\tм. Львів, вул. Стрийська, 10  ",
                contactInfo: " +380321234567, central@bibliogo.com\r\n");

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
            _libraryRepository.Verify(r => r.ExistsByNameAsync(ValidName, It.IsAny<CancellationToken>()), Times.Once);
            _libraryRepository.Verify(
                r => r.AddAsync(
                    It.Is<Library>(library => library.Name == ValidName
                        && library.Address == ValidAddress
                        && library.ContactInfo == ValidContactInfo),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddLibraryAsync_FieldsOnMaximumLength_ReturnsSuccess()
        {
            // Arrange
            SetupAdminAndAdd();
            var command = CreateCommand(
                name: new string(FillerCharacter, 100),
                address: new string(FillerCharacter, 200),
                contactInfo: new string(FillerCharacter, 200));

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Librarian)]
        public async Task AddLibraryAsync_ActorIsNotAdministrator_ReturnsFailureAndDoesNotSave(UserRole actorRole)
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(AdminId))
                .ReturnsAsync(new User { Id = AdminId, Role = actorRole });

            // Act
            var result = await _service.AddLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyNothingSaved();
        }

        [Fact]
        public async Task AddLibraryAsync_ActorNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(AdminId)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.AddLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            VerifyNothingSaved();
        }

        [Fact]
        public async Task AddLibraryAsync_LibraryWithSameNameExists_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            SetupAdmin();
            _libraryRepository
                .Setup(r => r.ExistsByNameAsync(ValidName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.AddLibraryAsync(CreateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _libraryRepository.Verify(r => r.AddAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, ValidName, ValidAddress, ValidContactInfo)]
        [InlineData(-5, ValidName, ValidAddress, ValidContactInfo)]
        [InlineData(AdminId, "", ValidAddress, ValidContactInfo)]
        [InlineData(AdminId, "   ", ValidAddress, ValidContactInfo)]
        [InlineData(AdminId, "А", ValidAddress, ValidContactInfo)]
        [InlineData(AdminId, ValidName, "", ValidContactInfo)]
        [InlineData(AdminId, ValidName, "   ", ValidContactInfo)]
        [InlineData(AdminId, ValidName, ValidAddress, "")]
        [InlineData(AdminId, ValidName, ValidAddress, "   ")]
        public async Task AddLibraryAsync_InvalidData_ReturnsFailureAndSkipsRepositories(
            int actorId, string name, string address, string contactInfo)
        {
            // Arrange
            var command = CreateCommand(actorId: actorId, name: name, address: address, contactInfo: contactInfo);

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        [Fact]
        public async Task AddLibraryAsync_NameTooLong_ReturnsFailureAndSkipsRepositories()
        {
            // Arrange
            var command = CreateCommand(name: new string(FillerCharacter, 101));

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        [Fact]
        public async Task AddLibraryAsync_AddressTooLong_ReturnsFailureAndSkipsRepositories()
        {
            // Arrange
            var command = CreateCommand(address: new string(FillerCharacter, 201));

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        [Fact]
        public async Task AddLibraryAsync_ContactInfoTooLong_ReturnsFailureAndSkipsRepositories()
        {
            // Arrange
            var command = CreateCommand(contactInfo: new string(FillerCharacter, 201));

            // Act
            var result = await _service.AddLibraryAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            VerifyNothingSaved();
        }

        private static AddLibraryCommand CreateCommand(
            int actorId = AdminId,
            string name = ValidName,
            string address = ValidAddress,
            string contactInfo = ValidContactInfo) => new()
        {
            ActorId = actorId,
            Name = name,
            Address = address,
            ContactInfo = contactInfo
        };

        private void SetupAdmin()
        {
            _userRepository
                .Setup(r => r.FindByIdAsync(AdminId))
                .ReturnsAsync(new User { Id = AdminId, Email = "admin@mail.com", Role = UserRole.Administrator });
        }

        private void SetupAdminAndAdd()
        {
            SetupAdmin();
            _libraryRepository
                .Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _libraryRepository
                .Setup(r => r.AddAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewLibraryId);
        }

        private void VerifyNothingSaved()
        {
            _libraryRepository.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _libraryRepository.Verify(r => r.AddAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
