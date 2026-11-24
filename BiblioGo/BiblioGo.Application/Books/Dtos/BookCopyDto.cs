using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Books.Dtos
{
    // Use case 7: фізичний примірник книги для перегляду (інвентарний номер, філія, статус)
    public class BookCopyDto
    {
        public int BookCopyId { get; set; }
        public int InventoryNumber { get; set; }
        public string LibraryName { get; set; } = string.Empty;
        public BookCopyStatus Status { get; set; }
    }
}
