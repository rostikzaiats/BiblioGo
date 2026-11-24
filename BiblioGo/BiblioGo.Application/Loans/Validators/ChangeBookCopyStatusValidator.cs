using BiblioGo.Application.Loans.Commands;

namespace BiblioGo.Application.Loans.Validators
{
    public static class ChangeBookCopyStatusValidator
    {
        public static string? Validate(ChangeBookCopyStatusCommand command)
        {
            return ValidateId(command.InventoryNumber, "Вкажіть коректний інвентарний номер примірника")
                ?? ValidateId(command.LibrarianId, "Некоректний ідентифікатор бібліотекаря")
                ?? ValidateStatus(command);
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }

        private static string? ValidateStatus(ChangeBookCopyStatusCommand command)
        {
            return Enum.IsDefined(command.NewStatus) ? null : "Некоректний статус примірника";
        }
    }
}
