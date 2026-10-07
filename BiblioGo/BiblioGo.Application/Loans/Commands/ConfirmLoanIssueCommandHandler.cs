using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Loans.Commands
{
    public class ConfirmLoanIssueCommandHandler
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly IUserRepository _userRepository;

        public ConfirmLoanIssueCommandHandler(
            ILoanRepository loanRepository,
            IBookCopyRepository bookCopyRepository,
            IUserRepository userRepository)
        {
            _loanRepository = loanRepository;
            _bookCopyRepository = bookCopyRepository;
            _userRepository = userRepository;
        }

        // Loan: Requested -> Issued, IssueDate, DueDate; BookCopy: Available -> Issued
        public Task<Loan> Handle(ConfirmLoanIssueCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
