using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Domain.Entities;
using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookCopyRepository
    {
        // UC 1, 3, 4: отримати примірник за ідентифікатором
        Task<BookCopy?> GetByIdAsync(int bookCopyId);

        // UC 1: знайти вільний примірник книги у конкретній філії
        Task<BookCopy?> FindAvailableCopyAsync(int bookId, int libraryId);

        // UC 3: чи вільний примірник (статус Available)
        Task<bool> IsAvailableAsync(int bookCopyId);

        // UC 6: зберегти зміни примірника (статус)
        Task UpdateAsync(BookCopy bookCopy);

        // UC 6: змінити статус примірника
        Task SetStatusAsync(int bookCopyId, BookCopyStatus status);
    }
}
