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

using System.Text;

namespace Crypter.CryptoLib.SodiumLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/SealedPublicKeyBox.cs
   /// </summary>
   public static class SealedPublicKeyBox
   {
      public static byte[] Create(byte[] data, byte[] recipientPublicKey)
      {
         return Sodium.SealedPublicKeyBox.Create(data, recipientPublicKey);
      }

      public static byte[] Create(string message, byte[] recipientPublicKey)
      {
         byte[] data = Encoding.UTF8.GetBytes(message);
         return Create(data, recipientPublicKey);
      }

      public static byte[] Open(byte[] ciphertext, byte[] recipientPrivateKey)
      {
         byte[] recipientPublicKey = ScalarMult.GetPublicKey(recipientPrivateKey);
         return Sodium.SealedPublicKeyBox.Open(ciphertext, recipientPrivateKey, recipientPublicKey);
      }
   }
}
