using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateKeysRequest
   {
      public string EncryptedPrivateKeyBase64 { get; set; }
      public string PublicKey { get; set; }

      [JsonConstructor]
      public UpdateKeysRequest(string encryptedPrivateKeyBase64, string publicKey)
      {
         EncryptedPrivateKeyBase64 = encryptedPrivateKeyBase64;
         PublicKey = publicKey;
      }
   }
}
