using BiblioGo.Application.Loans.Queries;

namespace BiblioGo.Application.Loans.Validators
{
    public static class CheckCopyAvailabilityValidator
    {
        public static string? Validate(CheckCopyAvailabilityQuery query)
        {
            return ValidateId(query.LoanId, "Вкажіть коректний номер заявки")
                ?? ValidateId(query.LibrarianId, "Некоректний ідентифікатор бібліотекаря");
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }
    }
}
