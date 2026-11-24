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
    public class UserServiceChangePasswordTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceChangePasswordTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance,
                new FakeCurrentUserSession());
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidCommand_ReturnsSuccessAndSavesNewHash()
        {
            // Arrange
            SetupStoredUserWithCorrectCurrentPassword();
            _passwordHasher.Setup(h => h.Hash("NewPass456")).Returns("new_hash");

            // Act
            var result = await _sut.ChangePasswordAsync(CreateChangePasswordCommand());

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u => u.Id == 7 && u.PasswordHash == "new_hash")),
                Times.Once());
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidCommand_DoesNotChangeOtherUserData()
        {
            // Arrange
            SetupStoredUserWithCorrectCurrentPassword();
            _passwordHasher.Setup(h => h.Hash("NewPass456")).Returns("new_hash");

            // Act
            var result = await _sut.ChangePasswordAsync(CreateChangePasswordCommand());

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u =>
                    u.Email == "reader@mail.com"
                    && u.FirstName == "Іван"
                    && u.LastName == "Петренко"
                    && u.Phone == "+380501234567"
                    && u.Role == UserRole.Reader)),
                Times.Once());
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify("OldPass123", "stored_hash")).Returns(false);

            // Act
            var result = await _sut.ChangePasswordAsync(CreateChangePasswordCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.ChangePasswordAsync(CreateChangePasswordCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Theory]
        [InlineData("", "NewPass456")]
        [InlineData("   ", "NewPass456")]
        [InlineData("OldPass123", "")]
        [InlineData("OldPass123", "short1")]
        [InlineData("OldPass123", "onlyletters")]
        [InlineData("OldPass123", "12345678")]
        [InlineData("OldPass123", "OldPass123")]
        public async Task ChangePasswordAsync_InvalidData_ReturnsFailureAndSkipsRepository(
            string currentPassword, string newPassword)
        {
            // Arrange
            var command = CreateChangePasswordCommand();
            command.CurrentPassword = currentPassword;
            command.NewPassword = newPassword;

            // Act
            var result = await _sut.ChangePasswordAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        private static ChangePasswordCommand CreateChangePasswordCommand() => new()
        {
            UserId = 7,
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass456"
        };

        private void SetupStoredUserWithCorrectCurrentPassword()
        {
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify("OldPass123", "stored_hash")).Returns(true);
        }
    }
}
