using BiblioGo.Application.Exceptions;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
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
        // ---------- Login (Use case 2) ----------

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUserDtoWithAllFields()
        {
            // Arrange
            var user = CreateStoredUser();
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.Verify("Password123", "stored_hash")).Returns(true);

            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.Equal(7, result.UserId);
            Assert.Equal("reader@mail.com", result.Email);
            Assert.Equal("Іван", result.FirstName);
            Assert.Equal("Петренко", result.LastName);
            Assert.Equal(UserRole.Reader, result.Role);
        }

        [Fact]
        public async Task LoginAsync_EmailWithSpacesAndUpperCase_SearchesByNormalizedEmail()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify("Password123", "stored_hash")).Returns(true);
            var command = CreateLoginCommand(email: "  Reader@Mail.COM  ");

            // Act
            await _sut.LoginAsync(command);

            // Assert
            _userRepository.Verify(r => r.FindByEmailAsync("reader@mail.com"), Times.Once());
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ThrowsBusinessRuleExceptionAndSkipsPasswordCheck()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.LoginAsync(CreateLoginCommand()));
            _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsBusinessRuleException()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), "stored_hash")).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.LoginAsync(CreateLoginCommand()));
        }

        [Fact]
        public async Task LoginAsync_UnknownEmailAndWrongPassword_ReturnSameErrorMessage()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync("unknown@mail.com")).ReturnsAsync((User?)null);
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), "stored_hash")).Returns(false);

            // Act
            var unknownEmailError = await Assert.ThrowsAsync<BusinessRuleException>(
                () => _sut.LoginAsync(CreateLoginCommand(email: "unknown@mail.com")));
            var wrongPasswordError = await Assert.ThrowsAsync<BusinessRuleException>(
                () => _sut.LoginAsync(CreateLoginCommand()));

            // Assert
            Assert.Equal(unknownEmailError.Message, wrongPasswordError.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_EmptyEmail_ThrowsInputValidationExceptionAndSkipsRepository(string email)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InputValidationException>(
                () => _sut.LoginAsync(CreateLoginCommand(email: email)));
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_EmptyPassword_ThrowsInputValidationExceptionAndSkipsRepository(string password)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InputValidationException>(
                () => _sut.LoginAsync(CreateLoginCommand(password: password)));
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task LoginAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync(null!));
        }

        [Fact]
        public async Task LoginAsync_RepositoryFails_RethrowsOriginalException()
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LoginAsync(CreateLoginCommand()));
            Assert.Equal("DB is down", ex.Message);
        }

        private static LoginUserCommand CreateLoginCommand(
            string email = "reader@mail.com",
            string password = "Password123")
        {
            return new LoginUserCommand { Email = email, Password = password };
        }

        private static User CreateStoredUser()
        {
            return new User
            {
                Id = 7,
                Email = "reader@mail.com",
                PasswordHash = "stored_hash",
                FirstName = "Іван",
                LastName = "Петренко",
                Phone = "+380501234567",
                Role = UserRole.Reader
            };
        }
    }
}