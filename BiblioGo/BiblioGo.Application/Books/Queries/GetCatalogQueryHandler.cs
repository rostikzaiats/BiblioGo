using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Queries
{
    public class GetCatalogQueryHandler
    {
        private readonly IBookRepository _bookRepository;

        public GetCatalogQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public Task<IEnumerable<Book>> Handle(GetCatalogQuery query)
        {
            return _bookRepository.GetCatalogAsync(
                query.Genre, query.Year, query.SortBy, query.PageNumber, query.PageSize);
        }
    }
}

