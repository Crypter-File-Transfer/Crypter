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
using Crypter.Crypto.Common.Padding;
using Crypter.Crypto.Common.StreamEncryption;

namespace Crypter.Crypto.Providers.Browser.Wrappers;

[SupportedOSPlatform("browser")]
public class StreamEncryptionFactory : IStreamEncryptionFactory
{
   private readonly IPadding _padding;

   public uint KeySize { get => 32; }
   public uint TagSize { get => 17; }

   public StreamEncryptionFactory(IPadding padding)
   {
      _padding = padding;
   }

   public IStreamEncrypt NewEncryptionStream(int padSize)
   {
      return new StreamEncrypt(_padding, padSize);
   }

   public IStreamDecrypt NewDecryptionStream(ReadOnlySpan<byte> key, ReadOnlySpan<byte> header)
   {
      return new StreamDecrypt(_padding, key, header);
   }
}