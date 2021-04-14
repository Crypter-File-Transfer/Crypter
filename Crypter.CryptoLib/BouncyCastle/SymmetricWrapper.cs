using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Text;

namespace Crypter.CryptoLib.BouncyCastle
{
   public class SymmetricWrapper
   {
      /// <summary>
      /// Generate a new AES256 key
      /// </summary>
      /// <remarks>Currently only supports the AES256 algorithm</remarks>
      /// <returns></returns>
      public KeyParameter GenerateSymmetricKey()
      {
         var generator = GeneratorUtilities.GetKeyGenerator("AES256");
         byte[] symmetricKey = generator.GenerateKey();
         return new KeyParameter(symmetricKey);
      }

      /// <summary>
      /// Generate a 128-bit IV
      /// </summary>
      /// <returns></returns>
      public byte[] GenerateIV()
      {
         SecureRandom random = new SecureRandom();
         return random.GenerateSeed(16);
      }

      /// <summary>
      /// Encrypt a sequence of bytes using AES/CTR/NoPadding
      /// </summary>
      /// <param name="inBytes"></param>
      /// <param name="key"></param>
      /// <param name="iv"></param>
      /// <returns></returns>
      public byte[] EncryptBytes(byte[] inBytes, KeyParameter key, byte[] iv)
      {
         IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
         cipher.Init(true, new ParametersWithIV(key, iv));
         return cipher.DoFinal(inBytes);
      }
   }
}
