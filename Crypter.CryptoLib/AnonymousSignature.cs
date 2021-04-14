namespace Crypter.CryptoLib
{
   public struct AnonymousSignature
   {
      public readonly string SignedDigest { get; }
      public readonly string SymmetricKey { get; }

      public AnonymousSignature(string signedDigestEncodedBase64, string symmetricKeyEncodedBase64)
      {
         SignedDigest = signedDigestEncodedBase64;
         SymmetricKey = symmetricKeyEncodedBase64;
      }
   }
}
