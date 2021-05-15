namespace Crypter.Contracts.Requests.Registered
{
    public class RegisterUserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisterUserRequest()
        { }

        public RegisterUserRequest(string username, string password, string email = null)
        {
            Username = username;
            Password = password;
            Email = email;
        }
    }
}
