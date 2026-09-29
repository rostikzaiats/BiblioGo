using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Loans.Commands
{
    public class ChangeBookCopyStatusCommandHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;
        private readonly IUserRepository _userRepository;

        public ChangeBookCopyStatusCommandHandler(IBookCopyRepository bookCopyRepository, IUserRepository userRepository)
        {
            _bookCopyRepository = bookCopyRepository;
            _userRepository = userRepository;
        }

        public Task Handle(ChangeBookCopyStatusCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
