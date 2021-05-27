using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
    public class Register
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(32, ErrorMessage = "Username cannot be more than 32 characters")]
        [RegularExpression(@"^\S*$", ErrorMessage = "Username cannot contain spaces")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Not a valid email address")]
        public string Email { get; set; }
    }
}
