namespace Crypter.Contracts.Requests.Registered
{
    public class UpdateUserRequest
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UpdateUserRequest()
        { }

        public UpdateUserRequest(string id, string email, string password, string token)
        {
            UserID = id; 
            Email = email;
            Password = password;
            Token = token; 
        }
    }
}
