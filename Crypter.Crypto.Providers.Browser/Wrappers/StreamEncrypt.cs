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
using BlazorSodium.Sodium;
using BlazorSodium.Sodium.Models;
using Crypter.Crypto.Common.Padding;
using Crypter.Crypto.Common.StreamEncryption;

namespace Crypter.Crypto.Providers.Browser.Wrappers
{
   [SupportedOSPlatform("browser")]
   public class StreamEncrypt : IStreamEncrypt
   {
      private readonly IPadding _padding;
      private readonly int _padSize;
      private StateAddress _stateAddress;

      public uint KeySize { get => SecretStream.KEY_BYTES; }
      public uint TagSize { get => SecretStream.A_BYTES; }

      public StreamEncrypt(IPadding padding, int padSize)
      {
         _padding = padding;
         _padSize = padSize;
      }

      public byte[] GenerateHeader(ReadOnlySpan<byte> key)
      {
         SecretStreamPushData initData = SecretStream.Crypto_SecretStream_XChaCha20Poly1305_Init_Push(key.ToArray());
         _stateAddress = initData.StateAddress;
         return initData.Header;
      }

      public byte[] Push(byte[] plaintext, bool final)
      {
         uint tag = final ? SecretStream.TAG_FINAL : SecretStream.TAG_MESSAGE;
         byte[] paddedPlaintext = _padding.Pad(plaintext, _padSize);
         return SecretStream.Crypto_SecretStream_XChaCha20Poly1305_Push(_stateAddress, paddedPlaintext, tag);
      }
   }
}
