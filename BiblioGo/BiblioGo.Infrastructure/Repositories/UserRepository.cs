using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;
using BiblioGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiblioGo.Infrastructure.Repositories
{

    public class UserRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : IUserRepository
    {
        public async Task<User?> FindByEmailAsync(string email)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> FindByIdAsync(int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> AddAsync(User user)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }
    }
}