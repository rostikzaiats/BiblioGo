using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Users.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Phone { get; set; } = string.Empty;
    }
}
