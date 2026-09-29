using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Commands
{
    public class UpdateBookCommandHandler
    {
        private readonly IBookRepository _bookRepository;

        public UpdateBookCommandHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<bool> Handle(UpdateBookCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
