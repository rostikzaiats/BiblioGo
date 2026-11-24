using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Dtos
{
    // Use case 6: результат зміни статусу примірника (який примірник і з якого статусу на який)
    public class BookCopyStatusChangeDto
    {
        public int BookCopyId { get; set; }
        public int InventoryNumber { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
        public BookCopyStatus PreviousStatus { get; set; }
        public BookCopyStatus NewStatus { get; set; }
    }
}
