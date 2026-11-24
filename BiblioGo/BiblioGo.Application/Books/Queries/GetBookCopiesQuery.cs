using BiblioGo.Domain.Enums;

namespace BiblioGo.Application.Books.Queries
{
    // Use case 7: переглянути примірники книги — інвентарні номери, філії та статуси.
    // LibraryId = null — усі філії, Status = null — примірники в усіх статусах
    public class GetBookCopiesQuery
    {
        public int BookId { get; set; }
        public int? LibraryId { get; set; }
        public BookCopyStatus? Status { get; set; }
    }
}
