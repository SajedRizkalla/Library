using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Data
{
    public class User
    {
        [Key]
        public string Username { get; set; }
        public string Email { get; set; }
        
        [MaxLength(256)]
        public string Password { get; set; }
        
        public string Gender { get; set; } 
        public bool IsAdmin { get; set; } = false; 
        
        // activation/deactivation
        public bool IsActive { get; set; } = true;
    }
}
