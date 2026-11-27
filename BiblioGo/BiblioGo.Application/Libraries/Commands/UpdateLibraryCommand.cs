namespace BiblioGo.Application.Libraries.Commands
{
    public class UpdateLibraryCommand
    {
        public int ActorId { get; set; }
        public int LibraryId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
    }
}