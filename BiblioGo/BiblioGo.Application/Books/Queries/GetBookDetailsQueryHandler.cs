using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Queries
{
    public class GetBookDetailsQueryHandler
    {
        private readonly IBookRepository _bookRepository;

        public GetBookDetailsQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<Book?> Handle(GetBookDetailsQuery query)
        {
            return _bookRepository.GetByIdAsync(query.BookId);
        }
    }
}
