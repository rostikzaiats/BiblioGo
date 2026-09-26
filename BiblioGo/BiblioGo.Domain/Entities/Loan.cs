using BiblioGo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblioGo.Domain.Entities
{
    public class Loan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int BookCopyId { get; set; }
        public BookCopy BookCopy { get; set; } = null!;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate
        { get; set; }
        public DateTime? ReturnDate { get; set;}
        public LoanStatus Status { get; set; }
    }
}
