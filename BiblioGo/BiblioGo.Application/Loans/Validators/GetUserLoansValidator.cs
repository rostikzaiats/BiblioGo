using BiblioGo.Application.Loans.Queries;

namespace BiblioGo.Application.Loans.Validators
{
    public static class GetUserLoansValidator
    {
        public static string? Validate(GetUserLoansQuery query)
        {
            if (query.UserId <= 0)
                return "Некоректний ідентифікатор користувача";
            if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
                return "Некоректний статус позики";

            return null;
        }
    }
}
