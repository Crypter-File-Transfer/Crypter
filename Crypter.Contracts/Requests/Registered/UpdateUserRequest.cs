using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class UpdateUserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PublicAlias { get; set; }
    }
}
