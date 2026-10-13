using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(
                 _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance);
        }

        [Fact]
        public async Task RegisterAsync_ValidCommand_ReturnsNewUserId()
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
            Assert.Equal(42, result);
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
        public async Task RegisterAsync_InvalidEmail_ThrowsInputValidationException(string invalidEmail)
        {
            // Arrange
            var command = CreateValidCommand();
            command.Email = invalidEmail;

            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterAsync(command));

            // Assert
            Assert.IsType<InputValidationException>(exception);
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Theory]
        [InlineData("")]
        [InlineData("1234567")]
        [InlineData("onlyletters")]
        [InlineData("123456789")]
        public async Task RegisterAsync_WeakPassword_ThrowsInputValidationException(string weakPassword)
        {
            // Arrange
            var command = CreateValidCommand();
            command.Password = weakPassword;

            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterAsync(command));

            // Assert
            Assert.IsType<InputValidationException>(exception);
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ThrowsBusinessRuleException()
        {
            // Arrange
            var command = CreateValidCommand();
            _userRepository
                .Setup(r => r.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(new User { Id = 1, Email = "test@example.com" });

            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterAsync(command));

            // Assert
            Assert.IsType<BusinessRuleException>(exception);
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task RegisterAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
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