using BiblioGo.Application.Loans.Queries;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Validators
{
    public static class GetLoanJournalValidator
    {
        public const int MaxPageNumber = 10_000;
        public const int MaxPageSize = 50;

        public static string? Validate(GetLoanJournalQuery query)
        {
            return ValidateId(query.LibrarianId, "Некоректний ідентифікатор бібліотекаря")
                ?? ValidateId(query.LibraryId, "Оберіть філію бібліотеки")
                ?? ValidateStatus(query.Status)
                ?? ValidateDates(query.DateFrom, query.DateTo)
                ?? ValidatePage(query.PageNumber, query.PageSize);
        }

        private static string? ValidateId(int id, string errorMessage)
        {
            return id > 0 ? null : errorMessage;
        }

        private static string? ValidateStatus(LoanStatus? status)
        {
            return status.HasValue && !Enum.IsDefined(status.Value) ? "Некоректний статус позики" : null;
        }

        // Дати розуміємо як календарні дати за локальним часом; «до» вмикається в діапазон разом із цілим днем
        private static string? ValidateDates(DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateTo.HasValue && dateTo.Value.Date >= DateTime.MaxValue.Date)
            {
                return "Некоректна дата «до»";
            }

            if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
            {
                return "Дата «від» не може бути пізніше за дату «до»";
            }

            return null;
        }

        private static string? ValidatePage(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageNumber > MaxPageNumber)
            {
                return $"Номер сторінки має бути від 1 до {MaxPageNumber}";
            }

            if (pageSize < 1 || pageSize > MaxPageSize)
            {
                return $"Кількість записів на сторінці має бути від 1 до {MaxPageSize}";
            }

            return null;
        }
    }
}
