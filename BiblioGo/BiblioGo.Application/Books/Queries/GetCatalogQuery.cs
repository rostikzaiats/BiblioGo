namespace BiblioGo.Application.Books.Queries
{
    public class GetCatalogQuery
    {
        public string? Genre { get; set; }
        public int? Year { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
