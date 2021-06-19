using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class UserAuthenticateResponse
   {
      public Guid Id { get; set; }
      public string Token { get; set; }
      public string EncryptedPrivateKey { get; set; }

      [JsonConstructor]
      public UserAuthenticateResponse(Guid id, string token, string encryptedPrivateKey = null)
      {
         Id = id;
         Token = token;
         EncryptedPrivateKey = encryptedPrivateKey;
      }
   }
}

