using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class RegisterUserRequest
   {
      public string Username { get; set; }
      public string Password { get; set; }
      public string Email { get; set; }

      [JsonConstructor]
      public RegisterUserRequest(string username, string password, string email = null)
      {
         Username = username;
         Password = password;
         Email = email;
      }
   }
}
