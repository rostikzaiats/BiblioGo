using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;

namespace BiblioGo.Application.Loans.Queries
{
    public class CheckCopyAvailabilityQueryHandler
    {
        private readonly IBookCopyRepository _bookCopyRepository;

        public CheckCopyAvailabilityQueryHandler(IBookCopyRepository bookCopyRepository)
        {
            _bookCopyRepository = bookCopyRepository;
        }

        // true — вільний екземпляр є на полиці (статус Available)
        public Task<bool> Handle(CheckCopyAvailabilityQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}
