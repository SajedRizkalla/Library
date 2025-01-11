using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyLibrary.Data
{
    public class Book
    {
        [Key]
        public string Id { get; set; }

        public string Cover { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }

        public string Publisher { get; set; }

        // If IsJustForSell is true, then RentPrice / RentQuantity is irrelevant (the book cannot be borrowed)
        public float Borrowprice { get; set; }
        public float Buyprice { get; set; }

        public int SellQuantity { get; set; } = 0; // Quantity available for selling
        public int RentQuantity { get; set; } = 0; // Quantity available for renting

        public int Year { get; set; }

        public float SalePercentage { get; set; } = 0; // Percentage discount (on Buyprice)
        public DateTime? SaleEndDate { get; set; } // End date for the discount

        public string Genre { get; set; }

        // NEW FIELD #1: If true => Book cannot be borrowed (only sold)
        public bool IsJustForSell { get; set; } = false;

        // NEW FIELD #2: Minimum age required. E.g., 18 for adult-only books, 0 for no restriction
        public int AgeLimit { get; set; } = 0;

        // Whether the book is on sale right now
        [NotMapped]
        public bool IsOnSale => 
            SalePercentage > 0 && 
            SaleEndDate.HasValue && 
            SaleEndDate.Value >= DateTime.Now;
        
        
        // URL to the PDF or base link for remote downloads
        public string DownloadUrl { get; set; }

        // In-memory property to represent the list
        public string RatingListJson { get; set; } = "[]"; // Initialize with empty JSON array

        [NotMapped]
        public List<Rating> RatingList
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(RatingListJson)
                        ? new List<Rating>()
                        : JsonSerializer.Deserialize<List<Rating>>(RatingListJson) ?? new List<Rating>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                    return new List<Rating>();
                }
            }
            set
            {
                try
                {
                    RatingListJson = JsonSerializer.Serialize(value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Serialization error: {ex.Message}");
                    RatingListJson = "[]";
                }
            }
        }

    }
}
