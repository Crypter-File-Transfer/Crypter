using System.ComponentModel.DataAnnotations;

namespace Crypter.Contracts.Requests.Registered
{
    public class AuthenticateUserRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AuthenticateUserRequest()
        { }

        public AuthenticateUserRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
