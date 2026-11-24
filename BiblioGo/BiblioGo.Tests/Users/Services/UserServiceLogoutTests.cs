using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static BiblioGo.Tests.Users.Services.UserTestData;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceLogoutTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly FakeCurrentUserSession _session = new();
        private readonly UserService _sut;

        public UserServiceLogoutTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance,
                _session);
        }

        [Fact]
        public void Logout_UserLoggedIn_ReturnsSuccess()
        {
            // Arrange
            _session.SignIn(CreateUserDto());

            // Act
            var result = _sut.Logout();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Logout_UserLoggedIn_ClearsSession()
        {
            // Arrange
            _session.SignIn(CreateUserDto());

            // Act
            _sut.Logout();

            // Assert
            Assert.Null(_session.User);
        }

        [Fact]
        public void Logout_NobodyLoggedIn_ReturnsFailure()
        {
            // Act
            var result = _sut.Logout();

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public void Logout_UserLoggedIn_DoesNotTouchRepository()
        {
            // Arrange
            _session.SignIn(CreateUserDto());

            // Act
            _sut.Logout();

            // Assert
            _userRepository.VerifyNoOtherCalls();
        }
    }
}