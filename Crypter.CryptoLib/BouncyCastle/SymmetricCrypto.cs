using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.BouncyCastle
{
   public class SymmetricCrypto
   {
      private readonly IBufferedCipher Cipher;

      public SymmetricCrypto()
      {
         Cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
      }

      public void InitializeForEncryption(KeyParameter key, byte[] iv)
      {
         Cipher.Init(true, new ParametersWithIV(key, iv));
      }

      public int GetOutputSize(int plaintextLength)
      {
         return Cipher.GetOutputSize(plaintextLength);
      }

      public int GetUpdateSize(int plaintextChunkLength)
      {
         return Cipher.GetUpdateOutputSize(plaintextChunkLength);
      }

      public byte[] EncryptChunk(byte[] plaintextChunk)
      {
         return Cipher.ProcessBytes(plaintextChunk);
      }

      public byte[] EncryptFinal(byte[] plaintextChunk)
      {
         var finalBytes = Cipher.DoFinal(plaintextChunk);
         Cipher.Reset();
         return finalBytes;
      }
   }
}
