using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static BiblioGo.Tests.Users.Services.UserTestData;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceUpdateUserTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceUpdateUserTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance,
                new FakeCurrentUserSession());
        }

        [Fact]
        public async Task UpdateUserAsync_ValidCommand_ReturnsSuccessAndSavesNewProfileData()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());

            // Act
            var result = await _sut.UpdateUserAsync(CreateUpdateCommand());

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u =>
                    u.Id == 7
                    && u.FirstName == "Олена"
                    && u.LastName == "Коваль"
                    && u.Phone == "+380931112233")),
                Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ValidCommand_DoesNotChangeEmailPasswordHashAndRole()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());

            // Act
            await _sut.UpdateUserAsync(CreateUpdateCommand());

            // Assert
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u =>
                    u.Email == "reader@mail.com"
                    && u.PasswordHash == "stored_hash"
                    && u.Role == UserRole.Reader)),
                Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_FieldsWithSurroundingSpaces_SavesTrimmedValues()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());
            var command = CreateUpdateCommand();
            command.FirstName = "  Олена  ";
            command.LastName = "  Коваль ";
            command.Phone = " +380931112233  ";

            // Act
            await _sut.UpdateUserAsync(command);

            // Assert
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u =>
                    u.FirstName == "Олена"
                    && u.LastName == "Коваль"
                    && u.Phone == "+380931112233")),
                Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_UserNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.UpdateUserAsync(CreateUpdateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Theory]
        [InlineData("", "Коваль", "+380931112233")]
        [InlineData("   ", "Коваль", "+380931112233")]
        [InlineData("О", "Коваль", "+380931112233")]
        [InlineData("Олена", "", "+380931112233")]
        [InlineData("Олена", "К", "+380931112233")]
        [InlineData("Олена", "Коваль", "")]
        [InlineData("Олена", "Коваль", "380931112233")]
        [InlineData("Олена", "Коваль", "+38093abc1122")]
        [InlineData("Олена", "Коваль", "+12345")]
        public async Task UpdateUserAsync_InvalidData_ReturnsFailureAndSkipsRepository(
            string firstName, string lastName, string phone)
        {
            // Arrange
            var command = CreateUpdateCommand();
            command.FirstName = firstName;
            command.LastName = lastName;
            command.Phone = phone;

            // Act
            var result = await _sut.UpdateUserAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        private static UpdateUserCommand CreateUpdateCommand() => new()
        {
            UserId = 7,
            FirstName = "Олена",
            LastName = "Коваль",
            Phone = "+380931112233"
        };
    }
}
