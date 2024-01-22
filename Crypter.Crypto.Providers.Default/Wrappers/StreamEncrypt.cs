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
using Geralt;
using static Geralt.IncrementalXChaCha20Poly1305;

namespace Crypter.Crypto.Providers.Default.Wrappers;

[UnsupportedOSPlatform("browser")]
public class StreamEncrypt : IStreamEncrypt
{
    private readonly IPadding _padding;
    private readonly int _padSize;
    private IncrementalXChaCha20Poly1305? _state;

    public uint KeySize
    {
        get => IncrementalXChaCha20Poly1305.KeySize;
    }

    public uint TagSize
    {
        get => IncrementalXChaCha20Poly1305.TagSize;
    }

    public StreamEncrypt(IPadding padding, int padSize)
    {
        _padding = padding;
        _padSize = padSize;
    }

    public byte[] GenerateHeader(ReadOnlySpan<byte> key)
    {
        byte[] header = new byte[HeaderSize];
        _state = new IncrementalXChaCha20Poly1305(false, header, key);
        return header;
    }

    public byte[] Push(byte[] plaintext, bool final)
    {
        if (_state is null)
        {
            throw new Exception($"{nameof(GenerateHeader)} must be invoked before pushing.");
        }
        
        ChunkFlag chunkFlag = final ? ChunkFlag.Final : ChunkFlag.Message;
        byte[] paddedPlaintext = _padding.Pad(plaintext, _padSize);
        byte[] ciphertext = new byte[paddedPlaintext.Length + TagSize];
        _state.Push(ciphertext, paddedPlaintext, chunkFlag);
        return ciphertext;
    }
}
