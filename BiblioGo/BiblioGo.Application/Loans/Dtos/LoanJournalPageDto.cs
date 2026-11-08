namespace BiblioGo.Application.Loans.Dtos
{
    // Use case 7: одна сторінка журналу позик і дані для перемикання сторінок
    public class LoanJournalPageDto
    {
        public IReadOnlyList<LoanJournalItemDto> Loans { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        // Скільки позик підходить під фільтр загалом і скільки це сторінок (0, якщо позик немає)
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
