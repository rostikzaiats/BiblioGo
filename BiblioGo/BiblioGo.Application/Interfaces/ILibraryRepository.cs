using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface ILibraryRepository
    {
        // Для випадайок: усі філії, відсортовані за назвою
        Task<IReadOnlyList<Library>> GetAllAsync(CancellationToken cancellationToken = default);

        // Фонд, use case 4 і 5: чи існує філія з таким ідентифікатором
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    }
}
