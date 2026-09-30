using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Queries
{
    public class CheckBookAvailabilityQueryHandler
    {
        private readonly IBookRepository _bookRepository;

        public CheckBookAvailabilityQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<int> Handle(CheckBookAvailabilityQuery query)
        {
            return _bookRepository.GetAvailableCopiesCountAsync(query.BookId);
        }
    }
}
