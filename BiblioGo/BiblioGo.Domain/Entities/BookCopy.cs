using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiblioGo.Domain.Enums;
namespace BiblioGo.Domain.Entities
{
    public class BookCopy
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;
        public int InventoryNumber { get; set; }
        public BookCopyStatus Status { get; set; }
        public int LibraryId { get; set; }
        public Library Library { get; set; } = null!;
    }
}
