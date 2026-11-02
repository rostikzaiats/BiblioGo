using BiblioGo.Application.Users.Commands;

namespace BiblioGo.Application.Users.Validators
{
    public static class ChangePasswordValidator
    {
        public static string? Validate(ChangePasswordCommand command)
        {
            return ValidateCurrentPassword(command.CurrentPassword)
                ?? RegisterUserValidator.ValidatePassword(command.NewPassword)
                ?? ValidatePasswordsDiffer(command.CurrentPassword, command.NewPassword);
        }

        private static string? ValidateCurrentPassword(string password)
        {
            return string.IsNullOrWhiteSpace(password) ? "Вкажіть поточний пароль" : null;
        }

        private static string? ValidatePasswordsDiffer(string currentPassword, string newPassword)
        {
            return currentPassword == newPassword
                ? "Новий пароль має відрізнятися від поточного"
                : null;
        }
    }
}
