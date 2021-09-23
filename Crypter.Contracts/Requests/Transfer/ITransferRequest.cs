namespace Crypter.Contracts.Requests
{
   public interface ITransferRequest
   {
      string CipherTextBase64 { get; set; }
      string SignatureBase64 { get; set; }
      string ClientEncryptionIVBase64 { get; set; }
      string ServerEncryptionKeyBase64 { get; set; }
      string X25519PublicKeyBase64 { get; set; }
      string Ed25519PublicKeyBase64 { get; set; }
   }
}
