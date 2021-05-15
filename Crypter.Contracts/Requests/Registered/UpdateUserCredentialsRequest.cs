namespace Crypter.Contracts.Requests.Registered
{
    public class UpdateUserCredentialsRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UpdateUserCredentialsRequest()
        { }

        public UpdateUserCredentialsRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}
