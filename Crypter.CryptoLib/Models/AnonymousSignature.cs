namespace Crypter.CryptoLib
{
   public class AnonymousSignature
   {
      public string Hash { get; }
      public string Key { get; }

      public AnonymousSignature(string signedDigestEncodedBase64, string symmetricKeyEncodedBase64)
      {
         Hash = signedDigestEncodedBase64;
         Key = symmetricKeyEncodedBase64;
      }
   }
}
