using System.ComponentModel.DataAnnotations;

namespace Crypter.Contracts.Requests.Registered
{
    public class AuthenticateUserRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
