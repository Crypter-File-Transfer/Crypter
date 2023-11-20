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
using Crypter.Core.Settings;
using HashidsNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Services;

public interface IHashIdService
{
    string Encode(Guid id);
    Guid Decode(string hash);
}

public static class HashIdServiceExtensions
{
    public static void AddHashIdService(this IServiceCollection services, Action<HashIdSettings> settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        services.Configure(settings);
        services.TryAddSingleton<IHashIdService, HashIdService>();
    }
}

public class HashIdService : IHashIdService
{
    private readonly Hashids _lib;

    public HashIdService(IOptions<HashIdSettings> settings)
    {
        _lib = new Hashids(settings.Value.Salt);
    }

    public string Encode(Guid id)
    {
        byte[] bytes = id.ToByteArray();
        string hexString = BitConverter.ToString(bytes).Replace("-", "");
        return _lib.EncodeHex(hexString);
    }

    public Guid Decode(string hash)
    {
        string hexString = _lib.DecodeHex(hash);

        int byteCount = hexString.Length / 2;
        byte[] bytes = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return new Guid(bytes);
    }
}
