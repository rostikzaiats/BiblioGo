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
    public class UserServiceLoginTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceLoginTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance);
        }

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
            Assert.Equal("+380501234567", dto.Phone);
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

        private static LoginUserCommand CreateLoginCommand(
            string email = "reader@mail.com",
            string password = "Password123")
        {
            return new LoginUserCommand { Email = email, Password = password };
        }
    }
}
