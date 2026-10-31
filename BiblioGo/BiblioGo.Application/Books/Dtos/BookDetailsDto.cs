namespace BiblioGo.Application.Books.Dtos
{
    // Use case 8: повна інформація про книгу для читача — опис і де її можна взяти
    public class BookDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Description { get; set; } = string.Empty;

        // Примірники на полицях (можна взяти зараз) і на руках у читачів; списані не враховуються
        public int AvailableCopies { get; set; }
        public int IssuedCopies { get; set; }

        // Філії, де є вільні примірники, з їх кількістю
        public IReadOnlyList<LibraryAvailabilityDto> AvailableInLibraries { get; set; } = [];
    }
}
