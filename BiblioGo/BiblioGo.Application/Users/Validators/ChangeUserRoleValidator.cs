using BiblioGo.Application.Users.Commands;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Users.Validators
{
    public static class ChangeUserRoleValidator
    {
        public static string? Validate(ChangeUserRoleCommand command)
        {
            return ValidateActorId(command.ActorId) ??
                ValidateTargetEmail(command.TargetEmail) ??
                ValidateNewRole(command.NewRole);
        }
        public static string? ValidateActorId(int actorId)
        {
            return actorId <= 0 ? "ActorId має бути додатнім числом" : null;
        }
        public static string? ValidateTargetEmail(string targetEmail)
        {
            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                return "Вкажіть електронну пошту користувача";
            }

            return RegisterUserValidator.ValidateEmail(targetEmail);
        }
        public static string? ValidateNewRole(UserRole newRole)
        {
            return !Enum.IsDefined(newRole) ? "Вкажіть нову роль користувача" : null;
        }
    }
}
