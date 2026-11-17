using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Dtos
{
    // Use case 3: результат перевірки примірника, закріпленого за заявкою, перед видачею
    public class CopyAvailabilityDto
    {
        public int LoanId { get; set; }
        public int BookCopyId { get; set; }
        public int InventoryNumber { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
        public BookCopyStatus CopyStatus { get; set; }

        // true — вільний екземпляр є на полиці, книгу можна видавати
        public bool IsAvailable { get; set; }

        // Чому видати не можна; порожній рядок, якщо примірник вільний
        public string UnavailableReason { get; set; } = string.Empty;
    }
}
