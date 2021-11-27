using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class UserAuthenticateResponse
   {
      public Guid Id { get; set; }
      public string Token { get; set; }
      public TimeSpan TokenDuration { get; set; }
      public string EncryptedX25519PrivateKey { get; set; }
      public string EncryptedEd25519PrivateKey { get; set; }

      [JsonConstructor]
      public UserAuthenticateResponse(Guid id, string token, TimeSpan tokenDuration, string encryptedX25519PrivateKey = null, string encryptedEd25519PrivateKey = null)
      {
         Id = id;
         Token = token;
         TokenDuration = tokenDuration;
         EncryptedX25519PrivateKey = encryptedX25519PrivateKey;
         EncryptedEd25519PrivateKey = encryptedEd25519PrivateKey;
      }
   }
}

