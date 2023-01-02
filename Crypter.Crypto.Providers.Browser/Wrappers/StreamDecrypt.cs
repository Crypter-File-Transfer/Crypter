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

using BlazorSodium.Sodium;
using BlazorSodium.Sodium.Models;
using Crypter.Crypto.Common.Padding;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Runtime.Versioning;

namespace Crypter.Crypto.Providers.Browser.Wrappers
{
   [SupportedOSPlatform("browser")]
   public class StreamDecrypt : IStreamDecrypt
   {
      private readonly IPadding _padding;
      private readonly StateAddress _stateAddress;

      public StreamDecrypt(IPadding padding, ReadOnlySpan<byte> key, ReadOnlySpan<byte> header)
      {
         _padding = padding;
         _stateAddress = SecretStream.Crypto_SecretStream_XChaCha20Poly1305_Init_Pull(header.ToArray(), key.ToArray());
      }

      public uint KeySize => SecretStream.KEY_BYTES;

      public uint TagSize => SecretStream.A_BYTES;

      public byte[] Pull(ReadOnlySpan<byte> ciphertext, int paddingBlockSize, out bool final)
      {
         SecretStreamPullData pullData = SecretStream.Crypto_SecretStream_XChaCha20Poly1305_Pull(_stateAddress, ciphertext.ToArray());
         final = pullData.Tag == SecretStream.TAG_FINAL;
         return _padding.Unpad(pullData.Message.AsSpan(), paddingBlockSize);
      }
   }
}
