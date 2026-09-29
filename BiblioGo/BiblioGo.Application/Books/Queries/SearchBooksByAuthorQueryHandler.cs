using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Queries
{
    public class SearchBooksByAuthorQueryHandler
    {
        private readonly IBookRepository _bookRepository;

        public SearchBooksByAuthorQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<IEnumerable<Book>> Handle(SearchBooksByAuthorQuery query)
        {
            return _bookRepository.SearchByAuthorAsync(query.Author);
        }
    }
}