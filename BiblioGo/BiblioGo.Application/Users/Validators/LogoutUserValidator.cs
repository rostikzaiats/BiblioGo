using BiblioGo.Application.Users.Commands;

namespace BiblioGo.Application.Users.Validators
{
    public static class LogoutUserValidator
    {
        public static string? Validate(LogoutUserCommand command)
        {
            return ValidateUserId(command.UserId);
        }

        public static string? ValidateUserId(int userId)
        {
            return userId <= 0 ? "UserId має бути додатнім числом" : null;
        }
    }
}
