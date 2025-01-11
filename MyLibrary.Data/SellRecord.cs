using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data;

public class SellRecord
{
    [Key]
    public string Id { get; set; }  // Primary key (GUID or similar)
        
    public string BookId { get; set; }   // The purchased book
    public string Username { get; set; } // Who purchased it

    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; } = 1; // If user buys multiple copies
    public float UnitPrice { get; set; }   // The price per book at checkout

    // Possibly track total or discount if needed
}