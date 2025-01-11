using System;
using System.Collections.Generic;

namespace MyLibrary.Data
{
    public class Rating
    {
        public int Id { get; set; } // Primary Key

        public int? RatingValue { get; set; } // Nullable star rating (1–5)

        public string? Feedback { get; set; } // Nullable user feedback

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Default timestamp

        public bool IsWebsiteRating { get; set; } // Discriminator column

        public string? BookId { get; set; } // Non-nullable Foreign Key for book-specific ratings (nvarchar)

        public Book Book { get; set; } // Navigation property
    }
}
