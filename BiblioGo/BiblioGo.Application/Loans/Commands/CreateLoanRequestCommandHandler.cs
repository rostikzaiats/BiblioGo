using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Loans.Commands
{
    public class CreateLoanRequestCommandHandler
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IBookCopyRepository _bookCopyRepository;

        public CreateLoanRequestCommandHandler(ILoanRepository loanRepository, IBookCopyRepository bookCopyRepository)
        {
            _loanRepository = loanRepository;
            _bookCopyRepository = bookCopyRepository;
        }

        // Створює позику зі статусом Requested ("Очікує підтвердження")
        public Task<Loan> Handle(CreateLoanRequestCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
