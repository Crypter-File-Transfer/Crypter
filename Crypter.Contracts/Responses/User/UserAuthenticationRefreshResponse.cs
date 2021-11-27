using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class UserAuthenticationRefreshResponse
   {
      public string Token { get; set; }
      public TimeSpan TokenDuration { get; set; }

      [JsonConstructor]
      public UserAuthenticationRefreshResponse(string token, TimeSpan tokenDuration)
      {
         Token = token;
         TokenDuration = tokenDuration;
      }
   }
}
