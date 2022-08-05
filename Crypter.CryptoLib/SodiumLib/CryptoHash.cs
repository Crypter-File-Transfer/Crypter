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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypter.CryptoLib.SodiumLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/CryptoHash.cs
   /// </summary>
   public static class CryptoHash
   {
      public static byte[] Sha512(byte[] data)
      {
         return Sodium.CryptoHash.Sha512(data);
      }

      public static byte[] Sha512(string data)
      {
         byte[] dataBytes = Encoding.UTF8.GetBytes(data);
         return Sha512(dataBytes);
      }

      public static byte[] Sha512(List<byte[]> data)
      {
         byte[] joinedBytes = data
            .SelectMany(x => x)
            .ToArray();

         return Sha512(joinedBytes);
      }

      public static byte[] Sha256(byte[] data)
      {
         return Sodium.CryptoHash.Sha256(data);
      }

      public static byte[] Sha256(string data)
      {
         byte[] dataBytes = Encoding.UTF8.GetBytes(data);
         return Sha256(dataBytes);
      }

      public static byte[] Sha256(List<byte[]> data)
      {
         byte[] joinedBytes = data
            .SelectMany(x => x)
            .ToArray();

         return Sha256(joinedBytes);
      }
   }
}
