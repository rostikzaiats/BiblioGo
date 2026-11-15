using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;

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
    }
}
