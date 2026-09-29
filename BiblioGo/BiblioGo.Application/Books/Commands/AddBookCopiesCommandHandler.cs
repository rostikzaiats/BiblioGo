using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Application.Books.Commands
{
    public class AddBookCopiesCommandHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;

        public AddBookCopiesCommandHandler(IBookCopyRepository bookCopyRepository)
        {
            _bookCopyRepository = bookCopyRepository;
        }

        public Task<IEnumerable<BookCopy>> Handle(AddBookCopiesCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
