using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiblioGo.Application.Interfaces;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Infrastructure.Persistence
{
    public class BookRepository : IBookRepository
    {
        public Task<IEnumerable<Book>> GetCatalogAsync(string? genre, int? year, string? sortBy, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Book>> SearchByTitleAsync(string title)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Book>> SearchByAuthorAsync(string author)
        {
            throw new NotImplementedException();
        }

        public Task<Book?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetAvailableCopiesCountAsync(int bookId)
        {
            throw new NotImplementedException();
        }
    }
}