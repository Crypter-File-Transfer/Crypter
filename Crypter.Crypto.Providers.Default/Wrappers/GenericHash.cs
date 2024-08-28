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
using Crypter.Crypto.Common.GenericHash;
using Geralt;

namespace Crypter.Crypto.Providers.Default.Wrappers;

[UnsupportedOSPlatform("browser")]
public class GenericHash : IGenericHash
{
    public uint KeySize
    {
        get => BLAKE2b.KeySize;
    }

    public byte[] GenerateHash(uint size, ReadOnlySpan<byte> data, ReadOnlySpan<byte> key = default)
    {
        byte[] buffer = new byte[size];

        if (key.IsEmpty)
        {
            BLAKE2b.ComputeHash(buffer, data);
        }
        else
        {
            BLAKE2b.ComputeTag(buffer, data, key);
        }

        return buffer;
    }

    public byte[] GenerateHash(uint size, string data, ReadOnlySpan<byte> key = default)
    {
        byte[] buffer = new byte[size];
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        if (key.IsEmpty)
        {
            BLAKE2b.ComputeHash(buffer, dataBytes);
        }
        else
        {
            BLAKE2b.ComputeTag(buffer, dataBytes, key);
        }

        return buffer;
    }
}
