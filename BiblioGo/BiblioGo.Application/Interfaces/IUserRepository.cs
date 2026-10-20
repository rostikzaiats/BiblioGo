using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
