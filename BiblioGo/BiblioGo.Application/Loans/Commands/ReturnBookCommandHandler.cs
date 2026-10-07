using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Loans.Commands
{
    public class ReturnBookCommandHandler
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly IUserRepository _userRepository;

        public ReturnBookCommandHandler(
            ILoanRepository loanRepository,
            IBookCopyRepository bookCopyRepository,
            IUserRepository userRepository)
        {
            _loanRepository = loanRepository;
            _bookCopyRepository = bookCopyRepository;
            _userRepository = userRepository;
        }

        // Loan: Issued -> Returned, ReturnDate; BookCopy: Issued -> Available
        public Task<Loan> Handle(ReturnBookCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
