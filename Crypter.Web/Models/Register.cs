using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
    public class Register
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string Email { get; set; }
    }
}
