using BiblioGo.Application.Libraries.Commands;

namespace BiblioGo.Application.Libraries.Validators
{
    public static class AddLibraryValidator
    {
        public const int MinNameLength = 2;
        public const int MaxNameLength = 100;
        public const int MaxAddressLength = 200;
        public const int MaxContactInfoLength = 200;

        public static string? Validate(AddLibraryCommand command)
        {
            return ValidateActorId(command.ActorId)
                ?? ValidateName(command.Name)
                ?? ValidateAddress(command.Address)
                ?? ValidateContactInfo(command.ContactInfo);
        }

        public static string? ValidateActorId(int actorId)
        {
            return actorId <= 0 ? "ActorId має бути додатнім числом" : null;
        }

        public static string? ValidateName(string name)
        {
            var trimmed = name.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть назву філії";
            if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
                return $"Назва філії має містити від {MinNameLength} до {MaxNameLength} символів";

            return null;
        }

        public static string? ValidateAddress(string address)
        {
            var trimmed = address.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть адресу філії";
            if (trimmed.Length > MaxAddressLength)
                return $"Адреса не може бути довшою за {MaxAddressLength} символів";

            return null;
        }

        public static string? ValidateContactInfo(string contactInfo)
        {
            var trimmed = contactInfo.Trim();

            if (trimmed.Length == 0)
                return "Вкажіть контактну інформацію філії";
            if (trimmed.Length > MaxContactInfoLength)
                return $"Контактна інформація не може бути довшою за {MaxContactInfoLength} символів";

            return null;
        }
    }
}
