namespace BiblioGo.Application.Books.Queries
{
    // Use case «Переглянути книги, доступні в бібліотеці»: у якій філії шукати книги з вільними примірниками
    public class GetLibraryBooksQuery
    {
        public int LibraryId { get; set; }
    }
}
