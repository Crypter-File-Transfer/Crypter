using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateUserCredentialsRequest
   {
      public string Email { get; set; }
      public string Password { get; set; }

      [JsonConstructor]
      public UpdateUserCredentialsRequest(string email, string password)
      {
         Email = email;
         Password = password;
      }
   }
}
