namespace BiblioGo.Application.Books.Queries
{
    // Use case 1: параметри перегляду каталогу — фільтр за жанром і роком, сортування, сторінка.
    // Рік приходить текстом з поля вводу, перевіряє і перетворює його сервіс
    public class GetCatalogQuery
    {
        public string? Genre { get; set; }
        public string? Year { get; set; }
        public CatalogSortBy SortBy { get; set; } = CatalogSortBy.Title;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
