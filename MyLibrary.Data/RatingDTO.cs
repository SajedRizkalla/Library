using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary.Data
{
    public class RatingDTO
    {
        [Range(0, 5)]
        public int? RatingValue { get; set; } // 1–5

        [StringLength(500)]
        public string? Feedback { get; set; } // User feedback

        public string? BookId { get; set; } // Associated book ID
    }

}
