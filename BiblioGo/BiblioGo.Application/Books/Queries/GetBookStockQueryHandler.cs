using System.Threading.Tasks;
using BiblioGo.Application.Books.Dtos;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Books.Queries
{
    public class GetBookStockQueryHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;

        public GetBookStockQueryHandler(IBookCopyRepository bookCopyRepository)
        {
            _bookCopyRepository = bookCopyRepository;
        }

        public Task<BookStockDto?> Handle(GetBookStockQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}
