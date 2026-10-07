using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Commands
{
    public class WriteOffBookCopyCommandHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;

        public WriteOffBookCopyCommandHandler(IBookCopyRepository bookCopyRepository)
        {
            _bookCopyRepository = bookCopyRepository;
        }

        public Task<bool> Handle(WriteOffBookCopyCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
