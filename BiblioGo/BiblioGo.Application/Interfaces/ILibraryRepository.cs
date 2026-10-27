using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface ILibraryRepository
    {
        // Для випадайок: усі філії, відсортовані за назвою
        Task<IReadOnlyList<Library>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
