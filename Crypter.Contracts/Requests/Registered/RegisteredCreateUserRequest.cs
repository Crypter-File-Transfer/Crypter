namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredCreateUserRequest
    {
        public string UserName { get; set; }
        //not required
        public string Email { get; set; }
        private string Password { get; set; }
    }
}
