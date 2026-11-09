using BiblioGo.Domain.Enums;
namespace BiblioGo.Application.Users.Commands
{
    public class ChangeUserRoleCommand
    {
        public int ActorId { get; set; }
        public string TargetEmail { get; set; } = string.Empty;
        public UserRole NewRole { get; set; }
    }
}
