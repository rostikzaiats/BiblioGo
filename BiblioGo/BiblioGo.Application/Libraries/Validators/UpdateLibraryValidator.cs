using BiblioGo.Application.Libraries.Commands;

namespace BiblioGo.Application.Libraries.Validators
{
    public static class UpdateLibraryValidator
    {
        public static string? Validate(UpdateLibraryCommand command)
        {
            return AddLibraryValidator.ValidateActorId(command.ActorId) ??
                   AddLibraryValidator.ValidateAddress(command.Address) ??
                   AddLibraryValidator.ValidateContactInfo(command.ContactInfo) ??
                   ValidateLibraryId(command.LibraryId);
        }

        public static string? ValidateLibraryId(int libraryId)
        {
            return libraryId <= 0 ? "Library id має бути додатнім числом!" : null;
        }
    }
}