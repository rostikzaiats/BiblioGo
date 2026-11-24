using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface ILibraryRepository
    {
        // Для випадайок: усі філії, відсортовані за назвою
        Task<IReadOnlyList<Library>> GetAllAsync(CancellationToken cancellationToken = default);

        // Фонд, use case 4 і 5: чи існує філія з таким ідентифікатором
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        // Адміністрування, use case 1: чи є вже філія з такою назвою (без урахування регістру)
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

        // Адміністрування, use case 1: додати філію до мережі, повертає ідентифікатор створеної філії
        Task<int> AddAsync(Library library, CancellationToken cancellationToken = default);
    }
}
