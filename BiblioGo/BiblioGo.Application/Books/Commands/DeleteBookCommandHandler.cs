using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Commands
{
    public class DeleteBookCommandHandler
    {
        private readonly IBookRepository _bookRepository;

        public DeleteBookCommandHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<bool> Handle(DeleteBookCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
