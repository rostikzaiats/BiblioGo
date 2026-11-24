using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;
using BiblioGo.Application.Interfaces;
using BiblioGo.Application.Users.Dtos;
namespace BiblioGo.Tests.Users.Services
{
    // Спільні тестові дані підсистеми «Авторизація та користувачі»
    internal static class UserTestData
    {
        public static User CreateStoredUser()
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
        public static UserDto CreateUserDto()
        {
            return new UserDto
            {
                UserId = 7,
                Email = "reader@mail.com",
                FirstName = "Іван",
                LastName = "Петренко",
                Phone = "+380501234567",
                Role = UserRole.Reader
            };
        }
    }
    internal sealed class FakeCurrentUserSession : ICurrentUserSession
    {
        public UserDto? User { get; private set; }
        public void SignIn(UserDto user) => User = user;
        public void SignOut() => User = null;
    }
}
