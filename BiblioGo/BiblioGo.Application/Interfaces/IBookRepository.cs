
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookRepository
    {
        // Use case 1, 4, 5, 6, 7: перегляд каталогу з опційними фільтром, сортуванням і пагінацією
        Task<IEnumerable<Book>> GetCatalogAsync(string? genre, int? year, string? sortBy, int pageNumber, int pageSize);

        // Use case 2: пошук за назвою (входження підрядка без урахування регістру)
        Task<IReadOnlyList<Book>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default);

        // Use case 3: пошук за автором (входження підрядка без урахування регістру)
        Task<IReadOnlyList<Book>> SearchByAuthorAsync(string author, CancellationToken cancellationToken = default);

        // Для випадайок: усі книги, відсортовані за назвою та автором
        Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default);

        // Use case 8: книга за ідентифікатором (null, якщо такої немає)
        Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}