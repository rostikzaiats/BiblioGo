using System.Net.Mail;
using BiblioGo.Application.Users.Commands;

namespace BiblioGo.Application.Users.Validators
{
    public static class RegisterUserValidator
    {
        private const int MinPasswordLength = 8;
        private const int MaxEmailLength = 254;
        private const int MinNameLength = 2;
        private const int MaxNameLength = 50;
        private const int MinPhoneDigits = 10;
        private const int MaxPhoneDigits = 15;

        public static string? Validate(RegisterUserCommand command)
        {
            return ValidateEmail(command.Email)
                ?? ValidatePassword(command.Password)
                ?? ValidateName(command.FirstName, "ім'я")
                ?? ValidateName(command.LastName, "прізвище")
                ?? ValidatePhone(command.Phone);
        }

        internal static string? ValidateEmail(string email)
        {
            var trimmed = email.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть email";
            if (trimmed.Length > MaxEmailLength)
                return $"Email не може бути довшим за {MaxEmailLength} символів";

            var isValid = MailAddress.TryCreate(trimmed, out var parsed)
                && parsed.Address == trimmed
                && parsed.Host.Contains('.');

            return isValid ? null : "Некоректний формат email";
        }

        private static string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Вкажіть пароль";
            if (password.Length < MinPasswordLength)
                return $"Пароль має містити щонайменше {MinPasswordLength} символів";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Пароль має містити хоча б одну літеру і одну цифру";

            return null;
        }

        internal static string? ValidateName(string name, string fieldName)
        {
            var trimmed = name.Trim();

            if (trimmed.Length == 0)
                return $"Вкажіть {fieldName}";
            if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
                return $"Поле «{fieldName}» має містити від {MinNameLength} до {MaxNameLength} символів";

            return null;
        }

        internal static string? ValidatePhone(string phone)
        {
            var trimmed = phone.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть телефон";
            if (!trimmed.StartsWith('+'))
                return "Телефон має починатися з «+»";

            var digits = trimmed[1..];
            if (digits.Length < MinPhoneDigits
                || digits.Length > MaxPhoneDigits
                || !digits.All(char.IsAsciiDigit))
            {
                return $"Після «+» має бути від {MinPhoneDigits} до {MaxPhoneDigits} цифр";
            }

            return null;
        }
    }
}
