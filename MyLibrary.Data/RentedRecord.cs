using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data;

public class RentedRecord
{
    [Key]
    public string Id { get; set; }
    public string BookId { get; set; }
    public string Username { get; set; }
        
    /// <summary>
    /// The price at which user rented the book
    /// (e.g., from Book.Borrowprice at time of checkout).
    /// </summary>
    public float RentPrice { get; set; }  

    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// If you want to mark it returned in the DB, you can track it here.
    /// </summary>
    public bool IsReturned { get; set; } = false;

}