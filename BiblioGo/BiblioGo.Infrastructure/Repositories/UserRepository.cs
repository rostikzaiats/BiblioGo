using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{
    public class UserRepository(ApplicationDbContext context) : IUserRepository
    {
        public async Task<User?> FindByEmailAsync(string email) =>
            await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> FindByIdAsync(int id) =>
            await context.Users.FindAsync(id);

        public async Task<User> AddAsync(User user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
    }
}