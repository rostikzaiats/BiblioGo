using BiblioGo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblioGo.Application.Interfaces
{
    public interface IBookCopyRepository
    {
        Task<BookCopy?> GetFirstAvailableAsync(int bookId);
        Task UpdateAsync(BookCopy bookCopy);
    }
}
