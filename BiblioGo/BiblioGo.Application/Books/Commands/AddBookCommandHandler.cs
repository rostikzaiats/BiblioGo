using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Commands
{
    public class AddBookCommandHandler
    {
        private readonly IBookRepository _bookRepository;

        public AddBookCommandHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<int> Handle(AddBookCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
