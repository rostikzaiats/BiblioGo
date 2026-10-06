using BiblioGo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblioGo.Application.Interfaces
{
    public interface ILoanRepository
    {
        Task<IEnumerable<Loan>> GetActiveByUserIdAsync(int userId);

        Task AddAsync(Loan loan);

        Task UpdateAsync(Loan loan);
    }
}
