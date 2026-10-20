using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Dtos
{
    // Use case 2: позика читача з даними книги та термінами повернення
    public class LoanDto
    {
        public int LoanId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string LibraryName { get; set; } = string.Empty;
        public int InventoryNumber { get; set; }
        public LoanStatus Status { get; set; }

        // null, поки заявка очікує підтвердження: дати виставляє бібліотекар при видачі
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        // Книга видана, а граничну дату повернення вже минуло
        public bool IsOverdue { get; set; }
    }
}
