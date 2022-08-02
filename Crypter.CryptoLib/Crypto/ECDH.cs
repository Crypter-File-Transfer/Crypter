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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Collections.Generic;

namespace Crypter.CryptoLib.Crypto
{
   /// <summary>
   /// Elliptical curve, Diffie-Hellman
   /// </summary>
   /// <remarks>
   /// https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/X25519Test.cs
   /// </remarks>
   public class ECDH
   {
      public static AsymmetricCipherKeyPair GenerateKeys()
      {
         SecureRandom random = new();
         IAsymmetricCipherKeyPairGenerator kpGen = new X25519KeyPairGenerator();
         kpGen.Init(new X25519KeyGenerationParameters(random));
         return kpGen.GenerateKeyPair();
      }

      /// <summary>
      /// Derive a shared key using one party's private key and the other party's public key
      /// </summary>
      /// <param name="privateKey">Alice's private key</param>
      /// <param name="publicKey">Bob's public key</param>
      /// <returns>256-bit shared key</returns>
      /// <remarks>
      /// It is not recommended to use this shared key directly, since it is not truly random.
      /// Use 'DeriveSharedKeys()' instead; the keys are truly random.
      /// </remarks>
      public static byte[] DeriveSharedKey(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey)
      {
         X25519Agreement agreement = new();
         agreement.Init(privateKey);
         byte[] sharedSecret = new byte[agreement.AgreementSize];
         agreement.CalculateAgreement(publicKey, sharedSecret, 0);
         return sharedSecret;
      }

      /// <summary>
      /// Derive a pair of shared keys using one party's private key and the other party's public key
      /// </summary>
      /// <param name="keyPair">Alice's key pair</param>
      /// <param name="publicKey">Bob's public key</param>
      /// <returns>A ReceiveKey for Alice and a SendKey for Bob</returns>
      public static (byte[] ReceiveKey, byte[] SendKey) DeriveSharedKeys(AsymmetricCipherKeyPair keyPair, AsymmetricKeyParameter publicKey)
      {
         byte[] sharedKey = DeriveSharedKey(keyPair.Private, publicKey);
         byte[] keyPairPublicKeyBytes = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
         byte[] standalonePublicKeyBytes = ((X25519PublicKeyParameters)publicKey).GetEncoded();

         List<byte[]> receiveKeySeedData = new List<byte[]>
         {
            sharedKey,
            keyPairPublicKeyBytes,
            standalonePublicKeyBytes
         };

         List<byte[]> sendKeySeedData = new List<byte[]>
         {
            sharedKey,
            standalonePublicKeyBytes,
            keyPairPublicKeyBytes
         };

         byte[] receiveKey = CryptoHash.Sha256(receiveKeySeedData);
         byte[] sendKey = CryptoHash.Sha256(sendKeySeedData);

         return (receiveKey, sendKey);
      }

      /// <summary>
      /// Derive a single key from a pair of shared keys
      /// </summary>
      /// <param name="receiveKey"></param>
      /// <param name="sendKey"></param>
      /// <returns></returns>
      /// <remarks>
      /// The order of the provided keys does not matter.
      /// </remarks>
      public static byte[] DeriveKeyFromECDHDerivedKeys(byte[] receiveKey, byte[] sendKey)
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

         List<byte[]> seedData = new List<byte[]>
         {
            firstKey,
            secondKey
         };

         return CryptoHash.Sha256(seedData);
      }
   }
}
