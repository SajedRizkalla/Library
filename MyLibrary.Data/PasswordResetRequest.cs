using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data;

public class PasswordResetRequest
{
    [Key]
    public string Id { get; set; }

    public string Email { get; set; }

    public string Token { get; set; }

    public DateTime ExpiryDate { get; set; }
}