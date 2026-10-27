using BiblioGo.Application.Users.Commands;
namespace BiblioGo.Application.Users.Validators
{
    public static class UpdateUserValidator
    {
        public static string? Validate(UpdateUserCommand command)
        {
            return RegisterUserValidator.ValidateName(command.FirstName, "ім'я")
                ?? RegisterUserValidator.ValidateName(command.LastName, "прізвище")
                ?? RegisterUserValidator.ValidatePhone(command.Phone);
        }
    }
}
