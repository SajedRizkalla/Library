using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary.Data.DTOS
{
    public class BookDTOs
    {
        public class DeleteRequest
        {
            public string Id { get; set; }
        }
        
        public class MyBooksViewModel
        {
            public List<RentedRecord> RentedBooks { get; set; }
            public List<SellRecord> PurchasedBooks { get; set; }
            public Dictionary<string, List<Rating>> Ratings { get; set; }
            public Dictionary<string, Book> BookDict { get; set; }

        }

    }
}
