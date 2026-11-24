using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceRegisterTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceRegisterTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance,
                new FakeCurrentUserSession());
        }

        [Fact]
        public async Task RegisterAsync_ValidCommand_ReturnsSuccessWithNewUserId()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupEmailIsNotRegistered();
            _passwordHasher.Setup(h => h.Hash(command.Password)).Returns("hashed_password");
            _userRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns((User user) =>
                {
                    user.Id = 42;
                    return Task.FromResult(user);
                });

            // Act
            var result = await _sut.RegisterAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once());
        }

        [Fact]
        public async Task RegisterAsync_ValidCommand_SavesHashedPasswordNotRaw()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupEmailIsNotRegistered();
            SetupAddAsyncReturningSameUser();
            _passwordHasher.Setup(h => h.Hash("Password123")).Returns("SECRET_HASH_123");

            // Act
            await _sut.RegisterAsync(command);

            // Assert
            _userRepository.Verify(
                r => r.AddAsync(It.Is<User>(u => u.PasswordHash == "SECRET_HASH_123")),
                Times.Once());
        }

        [Fact]
        public async Task RegisterAsync_EmailWithSpacesAndUppercase_StoresNormalizedEmail()
        {
            // Arrange
            var command = CreateValidCommand();
            command.Email = "  User.TEST@Example.COM  ";
            SetupEmailIsNotRegistered();
            SetupAddAsyncReturningSameUser();
            _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

            // Act
            await _sut.RegisterAsync(command);

            // Assert
            _userRepository.Verify(
                r => r.AddAsync(It.Is<User>(u => u.Email == "user.test@example.com")),
                Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-email")]
        [InlineData("a@")]
        [InlineData("@b.com")]
        [InlineData("Ivan <ivan@example.com>")]
        public async Task RegisterAsync_InvalidEmail_ReturnsFailureAndDoesNotSave(string invalidEmail)
        {
            // Arrange
            var command = CreateValidCommand();
            command.Email = invalidEmail;

            // Act
            var result = await _sut.RegisterAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Theory]
        [InlineData("")]
        [InlineData("1234567")]
        [InlineData("onlyletters")]
        [InlineData("123456789")]
        public async Task RegisterAsync_WeakPassword_ReturnsFailureAndDoesNotSave(string weakPassword)
        {
            // Arrange
            var command = CreateValidCommand();
            command.Password = weakPassword;

            // Act
            var result = await _sut.RegisterAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            var command = CreateValidCommand();
            _userRepository
                .Setup(r => r.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(new User { Id = 1, Email = "test@example.com" });

            // Act
            var result = await _sut.RegisterAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        private static RegisterUserCommand CreateValidCommand() => new()
        {
            Email = "test@example.com",
            Password = "Password123",
            FirstName = "Іван",
            LastName = "Франко",
            Phone = "+380971234567"
        };

        private void SetupEmailIsNotRegistered() =>
            _userRepository
                .Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

        private void SetupAddAsyncReturningSameUser() =>
            _userRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns((User user) => Task.FromResult(user));
    }
}
