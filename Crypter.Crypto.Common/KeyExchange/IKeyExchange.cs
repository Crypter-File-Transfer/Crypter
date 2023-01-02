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

namespace Crypter.Crypto.Common.KeyExchange
{
   public interface IKeyExchange
   {
      uint SeedSize { get; }
      uint NonceSize { get; }
      uint ProofSize { get; }
      X25519KeyPair GenerateKeyPair();
      X25519KeyPair GenerateKeyPairDeterministic(ReadOnlySpan<byte> seed);
      byte[] GeneratePublicKey(ReadOnlySpan<byte> privateKey);
      byte[] GenerateSharedKey(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey);
      (byte[] encryptionKey, byte[] proof) GenerateEncryptionKey(uint keySize, ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> nonce);
      (byte[] decryptionKey, byte[] proof) GenerateDecryptionKey(uint keySize, ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> nonce);
   }
}
