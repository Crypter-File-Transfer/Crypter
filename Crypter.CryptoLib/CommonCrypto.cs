/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.CryptoLib.Crypto;

namespace Crypter.CryptoLib
{
   public static class CommonCrypto
   {
      public static byte[] DeriveSharedKeyFromECDHDerivedKeys(byte[] receiveKey, byte[] sendKey)
      {
         byte[] firstKey = receiveKey;
         byte[] secondKey = sendKey;

         for (int i = 0; i < receiveKey.Length; i++)
         {
            if (receiveKey[i] < sendKey[i])
            {
               firstKey = receiveKey;
               secondKey = sendKey;
               break;
            }

            if (receiveKey[i] > sendKey[i])
            {
               firstKey = sendKey;
               secondKey = receiveKey;
               break;
            }
         }

         var digestor = new SHA(Enums.SHAFunction.SHA256);
         digestor.BlockUpdate(firstKey);
         digestor.BlockUpdate(secondKey);
         return digestor.GetDigest();
      }
   }
}
