namespace Crypter.Contracts.Enum
{
   public enum UploadResult
   {
      Success = 0,
      InvalidFileName,
      InvalidContentType,
      InvalidCipherText,
      InvalidServerEncryptionKey,
      InvalidClientEncryptionIV,
      InvalidSignature,
      InvalidX25519PublicKey,
      InvalidEd25519PublicKey,
      BlockedByUserPrivacy,
      OutOfSpace,
      Unknown
   }
}
