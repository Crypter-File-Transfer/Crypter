namespace Crypter.CryptoLib
{
   public struct AnonymousSignature
   {
      public readonly string Hash { get; }
      public readonly string Key { get; }

      public AnonymousSignature(string signedDigestEncodedBase64, string symmetricKeyEncodedBase64)
      {
         Hash = signedDigestEncodedBase64;
         Key = symmetricKeyEncodedBase64;
      }
   }
}
