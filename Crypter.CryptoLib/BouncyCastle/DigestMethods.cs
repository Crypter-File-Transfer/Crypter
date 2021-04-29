using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto.Digests;
using System;

namespace Crypter.CryptoLib.BouncyCastle
{
   public static class DigestMethods
   {
      public static byte[] GetDigest(byte[] data, DigestAlgorithm algorithm)
      {
         return algorithm switch
         {
            DigestAlgorithm.SHA1 => GetDigest(data, new Sha1Digest()),
            DigestAlgorithm.SHA224 => GetDigest(data, new Sha224Digest()),
            DigestAlgorithm.SHA256 => GetDigest(data, new Sha256Digest()),
            _ => throw new NotImplementedException()
         };
      }

      private static byte[] GetDigest(byte[] data, GeneralDigest digestor)
      {
         digestor.BlockUpdate(data, 0, data.Length);
         byte[] hash = new byte[digestor.GetDigestSize()];
         digestor.DoFinal(hash, 0);
         return hash;
      }
   }
}
