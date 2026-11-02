
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookCopyRepository
    {
        // UC 1, 3, 4, 6: отримати примірник за ідентифікатором
        Task<BookCopy?> GetByIdAsync(int bookCopyId, CancellationToken cancellationToken = default);

        // UC 1: знайти вільний примірник книги у конкретній філії
        // (статус Available і без іншої незакритої заявки Requested на цей примірник)
        Task<BookCopy?> FindAvailableCopyAsync(int bookId, int libraryId, CancellationToken cancellationToken = default);

        // UC 3: чи вільний примірник (статус Available)
        Task<bool> IsAvailableAsync(int bookCopyId);

        // UC 4: додати quantity нових примірників (статус Available) книги у філію з новими унікальними
        // інвентарними номерами (NotFoundException, якщо книги або філії не існує)
        Task<IReadOnlyList<BookCopy>> AddCopiesAsync(int bookId, int libraryId, int quantity, CancellationToken cancellationToken = default);

        // Для випадайки списання: вільні (Available) примірники книги разом із філією, за інвентарним номером
        Task<IReadOnlyList<BookCopy>> GetAvailableByBookAsync(int bookId, CancellationToken cancellationToken = default);

        // UC 8 (Каталог): примірники книги, які можна взяти зараз (Available і без незакритої заявки Requested —
        // те саме правило, що й у FindAvailableCopyAsync), разом із філією
        Task<IReadOnlyList<BookCopy>> GetLoanableByBookAsync(int bookId, CancellationToken cancellationToken = default);

        // UC 8 (Каталог): кількість примірників книги на руках у читачів (статус Issued)
        Task<int> CountIssuedByBookAsync(int bookId, CancellationToken cancellationToken = default);

        // UC 6: зберегти зміни примірника (статус)
        Task UpdateAsync(BookCopy bookCopy);

        // UC 6: змінити статус примірника (NotFoundException, якщо примірника не існує)
        Task SetStatusAsync(int bookCopyId, BookCopyStatus status, CancellationToken cancellationToken = default);
    }
}
