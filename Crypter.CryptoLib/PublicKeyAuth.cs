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
using Sodium;

namespace Crypter.CryptoLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/PublicKeyAuth.cs
   /// </summary>
   public static class PublicKeyAuth
   {
      public static int PrivateKeyBytes
      { get { return Sodium.PublicKeyAuth.SecretKeyBytes; } }

      public static int PublicKeyBytes
      { get { return Sodium.PublicKeyAuth.PublicKeyBytes; } }

      public static int SignatureBytes
      { get { return Sodium.PublicKeyAuth.SignatureBytes; } }

      public static int SeedBytes
      { get { return Sodium.PublicKeyAuth.SeedBytes; } }

      public static AsymmetricKeyPair GenerateKeyPair()
      {
         KeyPair keyPair = Sodium.PublicKeyAuth.GenerateKeyPair();
         return new AsymmetricKeyPair(keyPair.PrivateKey, keyPair.PublicKey);
      }

      public static byte[] GenerateSeed()
      {
         return SodiumCore.GetRandomBytes(SeedBytes);
      }

      public static AsymmetricKeyPair GenerateSeededKeyPair(byte[] seed)
      {
         KeyPair keyPair = Sodium.PublicKeyAuth.GenerateKeyPair(seed);
         return new AsymmetricKeyPair(keyPair.PrivateKey, keyPair.PublicKey);
      }

      public static byte[] Sign(byte[] message, byte[] privateKey)
      {
         return Sodium.PublicKeyAuth.SignDetached(message, privateKey);
      }

      public static bool Verify(byte[] message, byte[] signature, byte[] publicKey)
      {
         return Sodium.PublicKeyAuth.VerifyDetached(signature, message, publicKey);
      }

      public static byte[] GetPublicKey(byte[] privateKey)
      {
         return Sodium.PublicKeyAuth.ExtractEd25519PublicKeyFromEd25519SecretKey(privateKey);
      }
   }
}
