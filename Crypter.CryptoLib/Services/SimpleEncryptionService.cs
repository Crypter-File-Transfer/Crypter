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

using Crypter.Common.Monads;
using Crypter.CryptoLib.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.CryptoLib.Services
{
   public interface ISimpleEncryptionService
   {
      byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext);
      byte[] Encrypt(byte[] key, byte[] iv, string plaintext);
      Task<List<byte[]>> EncryptStreamAsync(byte[] key, byte[] iv, Stream stream, long streamLength, int partSize, Maybe<Func<double, Task>> progressFunc);
      (byte[] ciphertext, byte[] iv) Encrypt(byte[] key, byte[] plaintext);
      (byte[] ciphertext, byte[] iv) Encrypt(byte[] key, string plaintext);
      byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext);
      string DecryptToString(byte[] key, byte[] iv, byte[] ciphertext);
   }

   public class SimpleEncryptionService : ISimpleEncryptionService
   {
      public byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
      {
         var encrypter = new AES();
         encrypter.Initialize(key, iv, true);
         return encrypter.ProcessFinal(plaintext);
      }

      public (byte[] ciphertext, byte[] iv) Encrypt(byte[] key, byte[] plaintext)
      {
         var iv = AES.GenerateIV();

         var encrypter = new AES();
         encrypter.Initialize(key, iv, true);
         return (encrypter.ProcessFinal(plaintext), iv);
      }

      public byte[] Encrypt(byte[] key, byte[] iv, string plaintext)
      {
         var encrypter = new AES();
         encrypter.Initialize(key, iv, true);
         return encrypter.ProcessFinal(Encoding.UTF8.GetBytes(plaintext));
      }

      public async Task<List<byte[]>> EncryptStreamAsync(byte[] key, byte[] iv, Stream stream, long streamLength, int partSize, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         var encrypter = new AES();
         encrypter.Initialize(key, iv, true);

         int partCount = Convert.ToInt32(Math.Ceiling(streamLength / (double)partSize));
         List<byte[]> encryptedParts = new(partCount);

         int bytesRead = 0;
         while (bytesRead + partSize < streamLength)
         {
            byte[] readBuffer = new byte[partSize];
            bytesRead += await stream.ReadAsync(readBuffer.AsMemory(0, partSize));

            byte[] encryptedPart = encrypter.ProcessPart(readBuffer);
            encryptedParts.Add(encryptedPart);

            await progressFunc.IfSomeAsync(async func =>
            {
               double progress = (double)bytesRead / streamLength;
               await func.Invoke(progress);
            });
         }

         int finalPlaintextLength = Convert.ToInt32(streamLength) - bytesRead;
         byte[] finalReadBuffer = new byte[finalPlaintextLength];
         await stream.ReadAsync(finalReadBuffer.AsMemory(0, finalPlaintextLength));

         byte[] finalEncryptedPart = encrypter.ProcessFinal(finalReadBuffer);
         encryptedParts.Add(finalEncryptedPart);
         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return encryptedParts;
      }

      public (byte[] ciphertext, byte[] iv) Encrypt(byte[] key, string plaintext)
      {
         var iv = AES.GenerateIV();

         var encrypter = new AES();
         encrypter.Initialize(key, iv, true);
         return (encrypter.ProcessFinal(Encoding.UTF8.GetBytes(plaintext)), iv);
      }

      public byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
      {
         var decrypter = new AES();
         decrypter.Initialize(key, iv, false);
         return decrypter.ProcessFinal(ciphertext);
      }

      public string DecryptToString(byte[] key, byte[] iv, byte[] ciphertext)
      {
         var decrypter = new AES();
         decrypter.Initialize(key, iv, false);
         var plaintext = decrypter.ProcessFinal(ciphertext);
         return Encoding.UTF8.GetString(plaintext);
      }
   }
}
