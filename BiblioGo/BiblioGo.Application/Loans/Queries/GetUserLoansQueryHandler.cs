using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Loans.Queries
{
    public class GetUserLoansQueryHandler
    {
        private readonly ILoanRepository _loanRepository;

        public GetUserLoansQueryHandler(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public Task<IEnumerable<Loan>> Handle(GetUserLoansQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}
