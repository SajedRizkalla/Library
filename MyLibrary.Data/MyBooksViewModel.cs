using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary.Data
{
    public class MyBooksViewModel
    {
        public List<RentedRecord> RentedBooks { get; set; }
        public List<SellRecord> PurchasedBooks { get; set; }
        public Dictionary<string, List<Rating>> Ratings { get; set; } // BookId -> List of Ratings
        public Dictionary<string, Book> BookDict { get; set; }
    }

}
