using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Commands
{
    // UC 6 (Бібліотекар): Змінити статус примірника
    // (include для "Підтвердити видачу" та "Оформити повернення")
    public class ChangeBookCopyStatusCommand
    {
        public int BookCopyId { get; set; }
        public BookCopyStatus NewStatus { get; set; }
        public int LibrarianId { get; set; }
    }
}
