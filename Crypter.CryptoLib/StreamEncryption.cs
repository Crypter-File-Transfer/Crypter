﻿/*
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

using Crypter.CryptoLib.Models;
using System.Text;

namespace Crypter.CryptoLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/StreamEncryption.cs
   /// </summary>
   public static class StreamEncryption
   {
      public static byte[] GenerateKey()
      {
         return Sodium.StreamEncryption.GenerateKey();
      }

      public static EncryptedBox Encrypt(byte[] data, byte[] key)
      {
         byte[] nonce = Sodium.StreamEncryption.GenerateNonce();
         byte[] ciphertext = Sodium.StreamEncryption.Encrypt(data, nonce, key);
         return new EncryptedBox(ciphertext, nonce);
      }

      public static EncryptedBox Encrypt(string data, byte[] key)
      {
         byte[] dataBytes = Encoding.UTF8.GetBytes(data);
         return Encrypt(dataBytes, key);
      }

      public static byte[] Decrypt(EncryptedBox data, byte[] key)
      {
         return Sodium.StreamEncryption.Decrypt(data.Ciphertext, data.Nonce, key);
      }

      public static byte[] Decrypt(byte[] ciphertext, byte[] nonce, byte[] key)
      {
         return Sodium.StreamEncryption.Decrypt(ciphertext, nonce, key);
      }
   }
}
