using Crypter.CryptoLib.Crypto;

namespace Crypter.CryptoLib
{
   public static class CommonCrypto
   {
      public static byte[] DeriveSharedKeyFromECDHDerivedKeys(byte[] receiveKey, byte[] sendKey)
      {
         byte[] firstKey = receiveKey;
         byte[] secondKey = sendKey;

         for (int i = 0; i < receiveKey.Length; i++)
         {
            if (receiveKey[i] < sendKey[i])
            {
               firstKey = receiveKey;
               secondKey = sendKey;
               break;
            }

            if (receiveKey[i] > sendKey[i])
            {
               firstKey = sendKey;
               secondKey = receiveKey;
               break;
            }
         }

         var digestor = new SHA(Enums.SHAFunction.SHA256);
         digestor.BlockUpdate(firstKey);
         digestor.BlockUpdate(secondKey);
         return digestor.GetDigest();
      }
   }
}
