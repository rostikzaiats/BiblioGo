using BiblioGo.Application.Loans.Commands;

namespace BiblioGo.Application.Loans.Validators
{
    public static class ConfirmLoanIssueValidator
    {
        public static string? Validate(ConfirmLoanIssueCommand command)
        {
            return ValidateId(command.LoanId, "Вкажіть коректний номер заявки")
                ?? ValidateId(command.LibrarianId, "Некоректний ідентифікатор бібліотекаря")
                ?? ValidateDueDate(command.DueDate);
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }

        // Граничну дату розуміємо як календарну дату за локальним часом: повернення має бути не раніше завтра
        private static string? ValidateDueDate(DateTime dueDate)
        {
            return dueDate.Date > DateTime.Now.Date ? null : "Дата повернення має бути пізніше за сьогоднішню";
        }
    }
}
