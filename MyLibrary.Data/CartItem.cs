using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data;

public class CartItem
{
    [Key]
    public string Id { get; set; } // Primary key (GUID, etc.)

    public string Username { get; set; } // The user who owns this cart item
    public string BookId { get; set; }

    // Distinguish buy vs. rent
    public bool IsForRent { get; set; } = false;

    // If buy => how many copies?
    public int BuyQuantity { get; set; } = 1;

    // If rent => how many days the user wants, or store other details
    public int RentDays { get; set; } = 0; // e.g., 30 by default

    // Possibly store the price at the time the user added to cart, etc.
    public DateTime DateAdded { get; set; } = DateTime.Now;
}