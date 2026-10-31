using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Dtos
{
    // Use case 1: одна сторінка каталогу і дані для перемикання сторінок
    public class CatalogPageDto
    {
        public IReadOnlyList<Book> Books { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        // Скільки книг підходить під фільтр загалом і скільки це сторінок (0, якщо книг немає)
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
