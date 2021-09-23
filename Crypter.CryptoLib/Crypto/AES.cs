using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
   public class AES
   {
      private readonly IBufferedCipher Cipher;

      public AES()
      {
         Cipher = CipherUtilities.GetCipher("AES/CTR/PKCS7Padding");
      }

      /// <summary>
      /// Generate a new AES key of the given size
      /// </summary>
      /// <returns></returns>
      public static KeyParameter GenerateKey(AesKeySize keySize)
      {
         var algorithm = $"AES{(int)keySize}";
         var generator = GeneratorUtilities.GetKeyGenerator(algorithm);
         byte[] symmetricKey = generator.GenerateKey();
         return new KeyParameter(symmetricKey);
      }

      /// <summary>
      /// Generate a 128-bit IV
      /// </summary>
      /// <remarks>
      /// Be aware that AES uses 128-bit block sizes.
      /// This is true for both AES128 and AES256.
      /// The size of the IV should be equal to the block size.
      /// </remarks>
      /// <returns>
      /// An array of 16 random bytes
      /// </returns>
      public static byte[] GenerateIV()
      {
         SecureRandom random = new SecureRandom();
         return random.GenerateSeed(16);
      }

      public void Initialize(byte[] key, byte[] iv, bool forEncryption)
      {
         var keyParam = new KeyParameter(key);
         Cipher.Init(forEncryption, new ParametersWithIV(keyParam, iv));
      }

      public int GetOutputSize(int inputLength)
      {
         return Cipher.GetOutputSize(inputLength);
      }

      public int GetUpdateOutputSize(int updateLength)
      {
         return Cipher.GetUpdateOutputSize(updateLength);
      }

      public byte[] ProcessChunk(byte[] chunk)
      {
         return Cipher.ProcessBytes(chunk);
      }

      public byte[] ProcessFinal(byte[] chunk)
      {
         var finalBytes = Cipher.DoFinal(chunk);
         Cipher.Reset();
         return finalBytes;
      }
   }
}
