using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class MessageUploadRequest
   {
      public string Subject { get; set; }
      public string CipherTextBase64 { get; set; }
      public string EncryptedSymmetricInfoBase64 { get; set; }
      public string SignatureBase64 { get; set; }
      public string ServerEncryptionKeyBase64 { get; set; }
      public string PublicKeyBase64 { get; set; }
      public string RecipientUsername { get; set; }

      [JsonConstructor]
      public MessageUploadRequest(string subject, string cipherTextBase64, string encryptedSymmetricInfoBase64, string signatureBase64, string serverEncryptionKeyBase64, string publicKeyBase64, string recipientUsername = null)
      {
         Subject = subject;
         CipherTextBase64 = cipherTextBase64;
         EncryptedSymmetricInfoBase64 = encryptedSymmetricInfoBase64;
         SignatureBase64 = signatureBase64;
         ServerEncryptionKeyBase64 = serverEncryptionKeyBase64;
         PublicKeyBase64 = publicKeyBase64;
         RecipientUsername = recipientUsername;
      }
   }
}
