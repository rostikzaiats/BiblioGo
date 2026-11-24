using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Loans.Commands
{
    // UC 6 (Бібліотекар): Змінити статус примірника
    // (include для "Підтвердити видачу" та "Оформити повернення").
    // Бібліотекар вводить інвентарний номер примірника (його видно в журналі позик і при перевірці наявності)
    public class ChangeBookCopyStatusCommand
    {
        public int InventoryNumber { get; set; }
        public BookCopyStatus NewStatus { get; set; }
        public int LibrarianId { get; set; }
    }
}
