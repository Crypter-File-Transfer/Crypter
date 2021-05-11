using System.ComponentModel.DataAnnotations;

namespace Crypter.Contracts.Requests.Registered
{
    public class RegisterUserRequest
    {
        //[Required]
        public string Username { get; set; }
        //[Required]
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
