namespace BiblioGo.Application.Lookups.Dtos
{
    // Елемент випадайки в UI: Id для команди, Name для показу користувачу
    public class LookupItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
