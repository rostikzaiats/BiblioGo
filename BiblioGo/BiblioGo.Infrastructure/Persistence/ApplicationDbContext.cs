using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BiblioGo.Domain.Entities;

namespace BiblioGo.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BookCopy> BookCopies => Set<BookCopy>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<Library> Libraries => Set<Library>();

        // ще 4 DbSet — сам допиши для Book, BookCopy, Loan, Library
    }
}
