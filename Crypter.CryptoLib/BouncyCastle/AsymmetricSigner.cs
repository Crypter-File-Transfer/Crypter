using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.BouncyCastle
{
   public class AsymmetricSigner
   {
      private ISigner Signer;

      public void Initialize(AsymmetricKeyParameter privateKey)
      {
         Signer = SignerUtilities.GetSigner("SHA256withRSA");
         Signer.Init(true, privateKey);
      }

      public void DigestChunk(byte[] plaintextChunk)
      {
         Signer.BlockUpdate(plaintextChunk, 0, plaintextChunk.Length);
      }

      public byte[] GenerateSignature()
      {
         var signature = Signer.GenerateSignature();
         Signer.Reset();
         return signature;
      }
   }
}
