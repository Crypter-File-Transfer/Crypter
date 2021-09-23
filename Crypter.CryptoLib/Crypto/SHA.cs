using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using System;

namespace Crypter.CryptoLib.Crypto
{
   public class SHA
   {
      private readonly IDigest Digestor;

      public SHA(SHAFunction function)
      {
         Digestor = function switch
         {
            SHAFunction.SHA1 => new Sha1Digest(),
            SHAFunction.SHA224 => new Sha224Digest(),
            SHAFunction.SHA256 => new Sha256Digest(),
            SHAFunction.SHA512 => new Sha512Digest(),
            _ => throw new NotImplementedException()
         };
      }

      public void BlockUpdate(byte[] data)
      {
         Digestor.BlockUpdate(data, 0, data.Length);
      }

      public byte[] GetDigest()
      {
         byte[] hash = new byte[Digestor.GetDigestSize()];
         Digestor.DoFinal(hash, 0);
         Digestor.Reset();
         return hash;
      }

      public bool CompareNewInputAgainstKnownDigest(byte[] newInput, byte[] knownDigest)
      {
         BlockUpdate(newInput);
         var newDigest = GetDigest();

         if (newDigest.Length != knownDigest.Length)
         {
            return false;
         }

         for (int i = 0; i < knownDigest.Length; i++)
         {
            if (!knownDigest[i].Equals(newDigest[i]))
            {
               return false;
            }
         }
         return true;
      }
   }
}
