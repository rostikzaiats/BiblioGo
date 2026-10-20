using BiblioGo.Application.Users.Commands;

namespace BiblioGo.Application.Users.Validators
{
    public static class LoginUserValidator
    {
        public static string? Validate(LoginUserCommand command)
        {
            return ValidateEmail(command.Email)
                ?? ValidatePassword(command.Password);
        }

        private static string? ValidateEmail(string email)
        {
            var trimmed = email.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть email";
         return null;
        }
        private static string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Вкажіть пароль";
            return null;
        }
    }
}
