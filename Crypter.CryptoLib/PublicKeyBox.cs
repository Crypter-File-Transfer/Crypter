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
using Crypter.CryptoLib.Models;
using Sodium;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.CryptoLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/PublicKeyBox.cs
   /// </summary>
   public static class PublicKeyBox
   {
      public static AsymmetricKeyPair GenerateKeyPair()
      {
         KeyPair keyPair = Sodium.PublicKeyBox.GenerateKeyPair();
         return new AsymmetricKeyPair(keyPair.PrivateKey, keyPair.PublicKey);
      }

      public static AsymmetricKeyPair GenerateSeededKeyPair(byte[] seed)
      {
         KeyPair keyPair = Sodium.PublicKeyBox.GenerateSeededKeyPair(seed);
         return new AsymmetricKeyPair(keyPair.PrivateKey, keyPair.PublicKey);
      }

      public static byte[] GetPublicKey(byte[] privateKey)
      {
         return Sodium.PublicKeyBox.GenerateKeyPair(privateKey).PublicKey;
      }

      public static EncryptedBox Encrypt(byte[] data, byte[] privateKey, byte[] publicKey)
      {
         byte[] nonce = Sodium.PublicKeyBox.GenerateNonce();
         byte[] ciphertext = Sodium.PublicKeyBox.Create(data, nonce, privateKey, publicKey);
         return new EncryptedBox(nonce, ciphertext);
      }

      public static EncryptedBox Encrypt(string data, byte[] privateKey, byte[] publicKey)
      {
         byte[] dataBytes = Encoding.UTF8.GetBytes(data);
         return Encrypt(dataBytes, privateKey, publicKey);
      }

      public static byte[] Decrypt(EncryptedBox data, byte[] privateKey, byte[] publicKey)
      {
         return Sodium.PublicKeyBox.Open(data.Ciphertext, data.Nonce, privateKey, publicKey);
      }

      public static async Task<List<EncryptedBox>> EncryptPartsAsync(List<byte[]> dataParts, byte[] privateKey, byte[] publicKey, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         List<EncryptedBox> boxes = new List<EncryptedBox>(dataParts.Count);

         for (int i = 0; i < dataParts.Count; i++)
         {
            boxes.Add(Encrypt(dataParts[i], privateKey, publicKey));

            await progressFunc.IfSomeAsync(async func =>
            {
               double progress = (double)i / dataParts.Count;
               await func.Invoke(progress);
            });
         }

         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return boxes;
      }

      public static async Task<List<byte[]>> DecryptPartsAsync(List<EncryptedBox> dataParts, byte[] privateKey, byte[] publicKey, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         List<byte[]> plaintextParts = new List<byte[]>(dataParts.Count);

         for (int i = 0; i < dataParts.Count; i++)
         {
            plaintextParts.Add(Decrypt(dataParts[i], privateKey, publicKey));

            await progressFunc.IfSomeAsync(async func =>
            {
               double progress = (double)i / dataParts.Count;
               await func.Invoke(progress);
            });
         }

         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return plaintextParts;
      }
   }
}
