using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Queries
{
    public class SearchBooksByTitleQueryHandler
    {
        private readonly IBookRepository _bookRepository;

        public SearchBooksByTitleQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<IEnumerable<Book>> Handle(SearchBooksByTitleQuery query)
        {
            return _bookRepository.SearchByTitleAsync(query.Title);
        }
    }
}

