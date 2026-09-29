using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Loans.Queries
{
    public class GetLoanJournalQueryHandler
    {
        private readonly ILoanRepository _loanRepository;

        public GetLoanJournalQueryHandler(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public Task<IEnumerable<Loan>> Handle(GetLoanJournalQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}
