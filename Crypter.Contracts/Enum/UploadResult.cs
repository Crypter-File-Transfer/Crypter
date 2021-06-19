namespace Crypter.Contracts.Enum
{
   public enum UploadResult
   {
      Success = 0,
      InvalidFileName,
      InvalidContentType,
      InvalidCipherText,
      InvalidServerEncryptionKey,
      InvalidEncryptedSymmetricInfo,
      InvalidSignature,
      InvalidPublicKey,
      BlockedByUserPrivacy,
      OutOfSpace,
      Unknown
   }
}
