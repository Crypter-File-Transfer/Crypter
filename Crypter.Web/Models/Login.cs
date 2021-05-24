using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
    public class Login
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
