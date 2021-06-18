using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateUserKeysRequest
   {
      public string EncryptedPrivateKeyBase64 { get; set; }
      public string PublicKey { get; set; }

      [JsonConstructor]
      public UpdateUserKeysRequest(string encryptedPrivateKeyBase64, string publicKey)
      {
         EncryptedPrivateKeyBase64 = encryptedPrivateKeyBase64;
         PublicKey = publicKey;
      }
   }
}
