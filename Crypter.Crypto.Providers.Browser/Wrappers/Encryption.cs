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
using System.Text;
using Crypter.Crypto.Common.Encryption;

namespace Crypter.Crypto.Providers.Browser.Wrappers
{
   [SupportedOSPlatform("browser")]
   public class Encryption : IEncryption
   {
      public uint KeySize { get => BlazorSodium.Sodium.SecretBox.KEY_BYTES; }

      public uint NonceSize { get => BlazorSodium.Sodium.SecretBox.NONCE_BYTES; }

      public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
      {
         return BlazorSodium.Sodium.AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Decrypt(ciphertext.ToArray(), nonce.ToArray(), key.ToArray(), (byte[])null);
      }

      public string DecryptToString(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
      {
         byte[] plaintext = BlazorSodium.Sodium.AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Decrypt(ciphertext.ToArray(), nonce.ToArray(), key.ToArray(), (byte[])null);
         return Encoding.UTF8.GetString(plaintext);
      }

      public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext)
      {
         return BlazorSodium.Sodium.AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Encrypt(plaintext.ToArray(), nonce.ToArray(), key.ToArray(), (byte[])null);
      }

      public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, string plaintext)
      {
         return BlazorSodium.Sodium.AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Encrypt(plaintext, nonce.ToArray(), key.ToArray(), (byte[])null);
      }
   }
}
