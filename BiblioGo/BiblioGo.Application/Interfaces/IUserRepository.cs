using BiblioGo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
