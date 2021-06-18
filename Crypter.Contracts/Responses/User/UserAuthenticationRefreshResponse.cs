using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserAuthenticationRefreshResponse
   {
      public string Token { get; set; }

      [JsonConstructor]
      public UserAuthenticationRefreshResponse(string token)
      {
         Token = token;
      }
   }
}
