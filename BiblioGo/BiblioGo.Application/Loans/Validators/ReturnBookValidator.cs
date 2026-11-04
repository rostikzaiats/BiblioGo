using BiblioGo.Application.Loans.Commands;

namespace BiblioGo.Application.Loans.Validators
{
    public static class ReturnBookValidator
    {
        public static string? Validate(ReturnBookCommand command)
        {
            return ValidateId(command.LoanId, "Вкажіть коректний номер заявки")
                ?? ValidateId(command.LibrarianId, "Некоректний ідентифікатор бібліотекаря");
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }
    }
}
