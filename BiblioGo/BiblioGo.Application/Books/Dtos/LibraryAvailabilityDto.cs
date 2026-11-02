namespace BiblioGo.Application.Books.Dtos
{
    // Use case 8: скільки вільних примірників книги є в конкретній філії
    public class LibraryAvailabilityDto
    {
        public string LibraryName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int AvailableCopies { get; set; }
    }
}
