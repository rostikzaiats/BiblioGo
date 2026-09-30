using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookRepository
    {
        // Use case 1, 4, 5, 6, 7: перегляд каталогу з опційними фільтром, сортуванням і пагінацією
        Task<IEnumerable<Book>> GetCatalogAsync(string? genre, int? year, string? sortBy, int pageNumber, int pageSize);

        // Use case 2: пошук за назвою
        Task<IEnumerable<Book>> SearchByTitleAsync(string title);

        // Use case 3: пошук за автором
        Task<IEnumerable<Book>> SearchByAuthorAsync(string author);

        // Use case 8: перегляд інформації про конкретну книгу
        Task<Book?> GetByIdAsync(int id);

        // Use case 9: перевірка доступності (кількість вільних примірників)
        Task<int> GetAvailableCopiesCountAsync(int bookId);
    }
}