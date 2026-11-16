using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static BiblioGo.Tests.Users.Services.UserTestData;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceLogoutTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceLogoutTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance);
        }

        [Fact]
        public async Task LogoutAsync_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());

            // Act
            var result = await _sut.LogoutAsync(CreateLogoutCommand());

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(r => r.FindByIdAsync(7), Times.Once());
        }

        [Fact]
        public async Task LogoutAsync_ExistingUser_DoesNotModifyUserData()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());

            // Act
            await _sut.LogoutAsync(CreateLogoutCommand());

            // Assert
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task LogoutAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.LogoutAsync(CreateLogoutCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task LogoutAsync_InvalidUserId_ReturnsFailureAndSkipsRepository(int userId)
        {
            // Act
            var result = await _sut.LogoutAsync(CreateLogoutCommand(userId));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
        }

        private static LogoutUserCommand CreateLogoutCommand(int userId = 7)
        {
            return new LogoutUserCommand { UserId = userId };
        }
    }
}
