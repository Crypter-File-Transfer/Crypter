/*
 * Copyright (C) 2023 Crypter File Transfer
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

using System;
using System.Runtime.Versioning;
using Crypter.Crypto.Common.KeyExchange;
using Crypter.Crypto.Common.StreamGenericHash;
using Geralt;

namespace Crypter.Crypto.Providers.Default.Wrappers
{
   [UnsupportedOSPlatform("browser")]
   public class KeyExchange : AbstractKeyExchange
   {
      public KeyExchange(IStreamGenericHashFactory streamGenericHashFactory)
         : base(streamGenericHashFactory) { }

      public override X25519KeyPair GenerateKeyPair()
      {
         byte[] publicKey = new byte[X25519.PublicKeySize];
         byte[] privateKey = new byte[X25519.PrivateKeySize];
         X25519.GenerateKeyPair(publicKey, privateKey);
         return new X25519KeyPair(privateKey, publicKey);
      }

      public override X25519KeyPair GenerateKeyPairDeterministic(ReadOnlySpan<byte> seed)
      {
         byte[] publicKey = new byte[X25519.PublicKeySize];
         byte[] privateKey = new byte[X25519.PrivateKeySize];
         X25519.GenerateKeyPair(publicKey, privateKey, seed);
         return new X25519KeyPair(privateKey, publicKey);
      }

      public override byte[] GeneratePublicKey(ReadOnlySpan<byte> privateKey)
      {
         byte[] publicKey = new byte[X25519.PublicKeySize];
         X25519.ComputePublicKey(publicKey, privateKey);
         return publicKey;
      }

      public override byte[] GenerateSharedKey(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
      {
         byte[] sharedSecret = new byte[X25519.SharedSecretSize];
         X25519.ComputeSharedSecret(sharedSecret, privateKey, publicKey);
         return sharedSecret;
      }
   }
}
