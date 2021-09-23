using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class MessageTransferRequest : ITransferRequest
   {
      public string Subject { get; set; }
      public string CipherTextBase64 { get; set; }
      public string SignatureBase64 { get; set; }
      public string ClientEncryptionIVBase64 { get; set; }
      public string ServerEncryptionKeyBase64 { get; set; }
      public string X25519PublicKeyBase64 { get; set; }
      public string Ed25519PublicKeyBase64 { get; set; }

      [JsonConstructor]
      public MessageTransferRequest(string subject, string cipherTextBase64, string signatureBase64, string clientEncryptionIVBase64, string serverEncryptionKeyBase64, string x25519PublicKeyBase64, string ed25519PublicKeyBase64)
      {
         Subject = subject;
         CipherTextBase64 = cipherTextBase64;
         SignatureBase64 = signatureBase64;
         ClientEncryptionIVBase64 = clientEncryptionIVBase64;
         ServerEncryptionKeyBase64 = serverEncryptionKeyBase64;
         X25519PublicKeyBase64 = x25519PublicKeyBase64;
         Ed25519PublicKeyBase64 = ed25519PublicKeyBase64;
      }
   }
}
