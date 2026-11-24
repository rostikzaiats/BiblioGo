namespace BiblioGo.Application.Libraries.Commands
{
    public class AddLibraryCommand
    {
        public int ActorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
    }
}
