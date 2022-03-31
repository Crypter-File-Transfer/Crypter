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
