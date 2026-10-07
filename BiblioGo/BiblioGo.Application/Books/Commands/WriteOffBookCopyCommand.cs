namespace BiblioGo.Application.Books.Commands
{
    // Use case 6: списати примірник (не можна списати виданий примірник)
    public class WriteOffBookCopyCommand
    {
        public int BookCopyId { get; set; }
    }
}
