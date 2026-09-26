using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblioGo.Domain.Entities
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author {  get; set; } 
        public string Genre { get; set; }
        public int Year { get; set; }
        public string Description { get; set; }
    }
}
