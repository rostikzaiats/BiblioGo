using BiblioGo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblioGo.Application.Interfaces
{
    // TODO: Result class to return Success\Exception
    public interface IUserRepository
    {
         User? FindByEmail(string email);
         User? FindById(int id);
         bool AddUser(User user);
         bool UpdateUser(User user);
    }
}
