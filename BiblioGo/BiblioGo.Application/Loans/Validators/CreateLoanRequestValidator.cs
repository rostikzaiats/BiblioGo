using BiblioGo.Application.Loans.Commands;

namespace BiblioGo.Application.Loans.Validators
{
    public static class CreateLoanRequestValidator
    {
        public static string? Validate(CreateLoanRequestCommand command)
        {
            return ValidateId(command.UserId, "Некоректний ідентифікатор користувача")
                ?? ValidateId(command.BookId, "Оберіть книгу")
                ?? ValidateId(command.LibraryId, "Оберіть філію бібліотеки");
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }
    }
}
