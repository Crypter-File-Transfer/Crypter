using Org.BouncyCastle.Crypto.Digests;

namespace Crypter.CryptoLib.BouncyCastle
{
   public static class HashWrapper
   {
      public static byte[] GetSha256Digest(byte[] data)
      {
         var sha256 = new Sha256Digest();
         sha256.BlockUpdate(data, 0, data.Length);
         byte[] hash = new byte[sha256.GetDigestSize()];
         sha256.DoFinal(hash, 0);
         return hash;
      }
   }
}
