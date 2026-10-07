using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Queries
{
    public class GetBookCopiesQueryHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;

        public GetBookCopiesQueryHandler(IBookCopyRepository bookCopyRepository)
        {
            _bookCopyRepository = bookCopyRepository;
        }

        public Task<IEnumerable<BookCopy>> Handle(GetBookCopiesQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}
