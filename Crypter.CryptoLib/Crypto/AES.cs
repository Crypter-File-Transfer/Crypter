/*
 * Copyright (C) 2022 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
   public class AES
   {
      private readonly IBufferedCipher _cipher;

      public AES()
      {
         _cipher = CipherUtilities.GetCipher("AES/CTR/PKCS7Padding");
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
         SecureRandom random = new();
         return random.GenerateSeed(16);
      }

      public void Initialize(byte[] key, byte[] iv, bool forEncryption)
      {
         var keyParam = new KeyParameter(key);
         _cipher.Init(forEncryption, new ParametersWithIV(keyParam, iv));
      }

      public int GetUpdateOutputSize(int inputLength)
      {
         return _cipher.GetUpdateOutputSize(inputLength);
      }

      public int GetOutputSize(int inputLength)
      {
         return _cipher.GetOutputSize(inputLength);
      }

      public byte[] ProcessPart(byte[] input)
      {
         return _cipher.ProcessBytes(input);
      }

      public int ProcessPart(byte[] input, int inputOffset, int length, byte[] output, int outputOffset)
      {
         return _cipher.ProcessBytes(input, inputOffset, length, output, outputOffset);
      }

      public byte[] ProcessFinal(byte[] input)
      {
         var finalBytes = _cipher.DoFinal(input);
         _cipher.Reset();
         return finalBytes;
      }

      public int EncryptFinal(byte[] input, int inputOffset, int length, byte[] output, int outputOffset)
      {
         int processedBytes = _cipher.DoFinal(input, inputOffset, length, output, outputOffset);
         _cipher.Reset();
         return processedBytes;
      }

      public byte[] DecryptFinal(byte[] input, int inputOffset, int length, byte[] output, int outputOffset)
      {
         int processedBytes = _cipher.DoFinal(input, inputOffset, length, output, outputOffset);
         _cipher.Reset();

         int finalSize = outputOffset + processedBytes;
         return output[0..finalSize];
      }
   }
}
