namespace BiblioGo.Application.Libraries.Dtos
{
    public class LibraryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address {get; set;} = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
    }
}