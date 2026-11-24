using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Commands;
using BiblioGo.Application.Users.Services;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BiblioGo.Tests.Users.Services
{
    public class UserServiceChangeUserRoleTests
    {
        private const int AdminId = 1;
        private const int TargetId = 2;
        private const string TargetEmail = "reader@mail.com";

        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly UserService _sut;

        public UserServiceChangeUserRoleTests()
        {
            _sut = new UserService(
                _userRepository.Object,
                _passwordHasher.Object,
                NullLogger<UserService>.Instance,
                new FakeCurrentUserSession());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_ValidCommand_ReturnsSuccessAndSavesNewRole()
        {
            // Arrange
            SetupAdminAndTarget(UserRole.Reader);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u => u.Id == TargetId && u.Role == UserRole.Librarian)),
                Times.Once());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_PromoteToAdministrator_ReturnsSuccess()
        {
            // Arrange
            SetupAdminAndTarget(UserRole.Librarian);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Administrator));

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u => u.Id == TargetId && u.Role == UserRole.Administrator)),
                Times.Once());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_ValidCommand_DoesNotChangeOtherUserData()
        {
            // Arrange
            SetupAdminAndTarget(UserRole.Reader);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(
                r => r.UpdateAsync(It.Is<User>(u =>
                    u.Email == TargetEmail
                    && u.PasswordHash == "stored_hash"
                    && u.FirstName == "Іван"
                    && u.LastName == "Петренко"
                    && u.Phone == "+380501234567")),
                Times.Once());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_EmailWithSpacesAndUppercase_SearchesByNormalizedEmail()
        {
            // Arrange
            SetupAdminAndTarget(UserRole.Reader);
            var command = CreateCommand(UserRole.Librarian);
            command.TargetEmail = "  READER@Mail.COM  ";

            // Act
            var result = await _sut.ChangeUserRoleAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
            _userRepository.Verify(r => r.FindByEmailAsync(TargetEmail), Times.Once());
        }

        [Theory]
        [InlineData(UserRole.Reader)]
        [InlineData(UserRole.Librarian)]
        public async Task ChangeUserRoleAsync_ActorIsNotAdministrator_ReturnsFailureAndDoesNotSave(UserRole actorRole)
        {
            // Arrange
            _userRepository
                .Setup(r => r.FindByIdAsync(AdminId))
                .ReturnsAsync(new User { Id = AdminId, Role = actorRole });

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_ActorNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            _userRepository.Setup(r => r.FindByIdAsync(AdminId)).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_TargetNotFound_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            SetupAdmin();
            _userRepository.Setup(r => r.FindByEmailAsync(TargetEmail)).ReturnsAsync((User?)null);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_AdminChangesOwnRole_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            var admin = SetupAdmin();
            _userRepository.Setup(r => r.FindByEmailAsync(TargetEmail)).ReturnsAsync(admin);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Reader));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            Assert.Equal(UserRole.Administrator, admin.Role);
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task ChangeUserRoleAsync_TargetAlreadyHasThisRole_ReturnsFailureAndDoesNotSave()
        {
            // Arrange
            SetupAdminAndTarget(UserRole.Librarian);

            // Act
            var result = await _sut.ChangeUserRoleAsync(CreateCommand(UserRole.Librarian));

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        [Theory]
        [InlineData(0, TargetEmail, UserRole.Librarian)]
        [InlineData(-5, TargetEmail, UserRole.Librarian)]
        [InlineData(AdminId, "", UserRole.Librarian)]
        [InlineData(AdminId, "   ", UserRole.Librarian)]
        [InlineData(AdminId, "invalid-email", UserRole.Librarian)]
        [InlineData(AdminId, TargetEmail, (UserRole)99)]
        public async Task ChangeUserRoleAsync_InvalidData_ReturnsFailureAndSkipsRepository(
            int actorId, string email, UserRole newRole)
        {
            // Arrange
            var command = new ChangeUserRoleCommand
            {
                ActorId = actorId,
                TargetEmail = email,
                NewRole = newRole
            };

            // Act
            var result = await _sut.ChangeUserRoleAsync(command);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            _userRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never());
            _userRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>()), Times.Never());
            _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never());
        }

        private static ChangeUserRoleCommand CreateCommand(UserRole newRole) => new()
        {
            ActorId = AdminId,
            TargetEmail = TargetEmail,
            NewRole = newRole
        };

        private User SetupAdmin()
        {
            var admin = new User
            {
                Id = AdminId,
                Email = "admin@mail.com",
                Role = UserRole.Administrator
            };
            _userRepository.Setup(r => r.FindByIdAsync(AdminId)).ReturnsAsync(admin);
            return admin;
        }

        private void SetupAdminAndTarget(UserRole targetRole)
        {
            SetupAdmin();
            _userRepository.Setup(r => r.FindByEmailAsync(TargetEmail)).ReturnsAsync(new User
            {
                Id = TargetId,
                Email = TargetEmail,
                PasswordHash = "stored_hash",
                FirstName = "Іван",
                LastName = "Петренко",
                Phone = "+380501234567",
                Role = targetRole
            });
        }
    }
}
