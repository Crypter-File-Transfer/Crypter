
namespace Crypter.Contracts.Requests.Registered
{
    public class AuthenticateUserRequest
    {
        public string Username { get; set; }
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
