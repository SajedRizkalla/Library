using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data;

public class WaitingList
{
    [Key]
    public string Id { get; set; } // Unique ID for the waiting list entry

    public string BookId { get; set; } // FK to Book
    public string Username { get; set; } // FK to User
    public DateTime AddedDate { get; set; } = DateTime.Now; // When the user was added to the list
}