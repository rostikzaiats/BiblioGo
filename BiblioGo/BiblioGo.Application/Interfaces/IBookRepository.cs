
using BiblioGo.Application.Books.Queries;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookRepository
    {
        // Use case 1: одна сторінка каталогу (skip/take) з опційними фільтрами за жанром і роком та сортуванням
        Task<IReadOnlyList<Book>> GetCatalogPageAsync(
            string? genre,
            int? year,
            CatalogSortBy sortBy,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        // Use case 1: скільки книг підходить під фільтри (для кількості сторінок)
        Task<int> CountCatalogAsync(string? genre, int? year, CancellationToken cancellationToken = default);

        // Use case 1: усі жанри каталогу для випадайки фільтра, за алфавітом
        Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);

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