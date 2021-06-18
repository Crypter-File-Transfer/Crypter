
using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class AuthenticateUserRequest
   {
      public string Username { get; set; }
      public string Password { get; set; }

      [JsonConstructor]
      public AuthenticateUserRequest(string username, string password)
      {
         Username = username;
         Password = password;
      }
   }
}
