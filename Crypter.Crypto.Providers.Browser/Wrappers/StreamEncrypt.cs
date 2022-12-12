﻿/*
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

using BlazorSodium.Sodium;
using BlazorSodium.Sodium.Models;
using Crypter.Crypto.Common.Padding;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Linq;
using System.Runtime.Versioning;

namespace Crypter.Crypto.Providers.Browser.Wrappers
{
   [SupportedOSPlatform("browser")]
   public class StreamEncrypt : IStreamEncrypt
   {
      private readonly IPadding _padding;
      private readonly short _blockSize;
      private StateAddress _stateAddress;

      public StreamEncrypt(IPadding padding, short blockSize)
      {
         _padding = padding;
         _blockSize = blockSize;
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
         bool meetsBlockSize = plaintext.Length % _blockSize == 0;
         if (!(final || meetsBlockSize))
         {
            throw new ArgumentOutOfRangeException(nameof(plaintext), plaintext.Length, $"{nameof(plaintext)} length must be divisible by {_blockSize}");
         }

         byte[] paddedPlaintext = final
            ? _padding.Pad(plaintext, _blockSize)
            : plaintext.ToArray();
         return SecretStream.Crypto_SecretStream_XChaCha20Poly1305_Push(_stateAddress, paddedPlaintext, tag);
      }
   }
}