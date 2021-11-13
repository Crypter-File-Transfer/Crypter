using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserEmailVerificationResponse
   {
      public bool Success { get; set; }

      [JsonConstructor]
      public UserEmailVerificationResponse(bool success)
      {
         Success = success;
      }
   }
}
