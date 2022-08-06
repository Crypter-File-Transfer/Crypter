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

using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;
using System.Collections.Generic;

namespace Crypter.CryptoLib
{
   public static class KDF
   {
      public static TransmissionKeyRing CreateTransmissionKeys(AsymmetricKeyPair senderKeys, byte[] recipientPublicKey, byte[] nonce)
      {
         byte[] sharedKey = ScalarMult.GetSharedKey(senderKeys.PrivateKey, recipientPublicKey);

         List<byte[]> receiveKeySeed = new List<byte[]> { sharedKey, senderKeys.PublicKey, recipientPublicKey, nonce };
         byte[] receiveKey = CryptoHash.Sha256(receiveKeySeed);

         List<byte[]> sendKeySeed = new List<byte[]> { sharedKey, recipientPublicKey, senderKeys.PublicKey, nonce };
         byte[] sendKey = CryptoHash.Sha256(sendKeySeed);

         byte[] serverProof = DeriveServerProof(receiveKey, sendKey, nonce);

         return new TransmissionKeyRing(sendKey, receiveKey, serverProof);
      }

      public static byte[] GenerateNonce()
      {
         return Random.RandomBytes(16);
      }

      private static byte[] DeriveServerProof(byte[] receiveKey, byte[] sendKey, byte[] nonce)
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

         List<byte[]> sortedKeys = new List<byte[]> { firstKey, secondKey, nonce };
         return CryptoHash.Sha512(sortedKeys);
      }
   }
}
