using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateContactInfoRequest
   {
      public string Email { get; set; }
      public string CurrentPassword { get; set; }

      [JsonConstructor]
      public UpdateContactInfoRequest(string email, string currentPassword)
      {
         Email = email;
         CurrentPassword = currentPassword;
      }
   }
}
