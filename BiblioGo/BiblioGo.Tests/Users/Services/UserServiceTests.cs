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

        [Fact]
        public async Task RegisterAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task RegisterAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.RegisterAsync(CreateValidCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
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
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithUserDtoWithAllFields()
        {
            // Arrange
            var user = CreateStoredUser();
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.Verify("Password123", "stored_hash")).Returns(true);

            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.True(result.IsSuccess);
            var dto = result.Value;
            Assert.Equal(7, dto.UserId);
            Assert.Equal("reader@mail.com", dto.Email);
            Assert.Equal("Іван", dto.FirstName);
            Assert.Equal("Петренко", dto.LastName);
            Assert.Equal(UserRole.Reader, dto.Role);
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
        public async Task LoginAsync_UnknownEmail_ReturnsFailureAndSkipsPasswordCheck()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsFailure()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), "stored_hash")).Returns(false);

            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        [Fact]
        public async Task LoginAsync_UnknownEmailAndWrongPassword_ReturnSameErrorMessage()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByEmailAsync("unknown@mail.com")).ReturnsAsync((User?)null);
            _userRepository.Setup(r => r.FindByEmailAsync("reader@mail.com")).ReturnsAsync(CreateStoredUser());
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), "stored_hash")).Returns(false);

            // Act
            var unknownEmailResult = await _sut.LoginAsync(CreateLoginCommand(email: "unknown@mail.com"));
            var wrongPasswordResult = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.True(unknownEmailResult.IsFailure);
            Assert.True(wrongPasswordResult.IsFailure);
            Assert.Equal(unknownEmailResult.Error, wrongPasswordResult.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_EmptyEmail_ReturnsFailureAndSkipsRepository(string email)
        {
            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand(email: email));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginAsync_EmptyPassword_ReturnsFailureAndSkipsRepository(string password)
        {
            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand(password: password));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task LoginAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync(null!));
        }

        [Fact]
        public async Task LoginAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.LoginAsync(CreateLoginCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        // ---------- UpdateUser (Use case 3) ----------

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

        [Fact]
        public async Task UpdateUserAsync_NullCommand_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.UpdateUserAsync(null!));
        }

        [Fact]
        public async Task UpdateUserAsync_FindByIdFails_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.UpdateUserAsync(CreateUpdateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task UpdateUserAsync_UpdateFails_ReturnsFailure()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync(CreateStoredUser());
            _userRepository
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .ThrowsAsync(new InvalidOperationException("DB is down"));

            // Act
            var result = await _sut.UpdateUserAsync(CreateUpdateCommand());

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        private static UpdateUserCommand CreateUpdateCommand() => new()
        {
            UserId = 7,
            FirstName = "Олена",
            LastName = "Коваль",
            Phone = "+380931112233"
        };

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