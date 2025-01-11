using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary.Data.DTOS
{
    public class UserDTOs
    {
        public class EditRequest
        {
          
                public string Username { get; set; }
                public string Email { get; set; }
                public string Password { get; set; }
                public string Gender { get; set; }

                public string OldUsername { get; set; }
        }
        
        public class ResetPasswordViewModel
        {
            public string Token { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }


    }
}
