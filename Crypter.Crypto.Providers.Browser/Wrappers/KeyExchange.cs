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
using Crypter.Crypto.Common.KeyExchange;
using Crypter.Crypto.Common.StreamGenericHash;

namespace Crypter.Crypto.Providers.Browser.Wrappers;

[SupportedOSPlatform("browser")]
public class KeyExchange : AbstractKeyExchange
{
    public KeyExchange(IStreamGenericHashFactory streamGenericHashFactory)
        : base(streamGenericHashFactory)
    {
    }

    public override X25519KeyPair GenerateKeyPair()
    {
        BlazorSodium.Sodium.Models.X25519KeyPair keyPair = BlazorSodium.Sodium.KeyExchange.Crypto_KX_KeyPair();
        return new X25519KeyPair(keyPair.PrivateKey, keyPair.PublicKey);
    }

    public override X25519KeyPair GenerateKeyPairDeterministic(ReadOnlySpan<byte> seed)
    {
        BlazorSodium.Sodium.Models.X25519KeyPair keyPair =
            BlazorSodium.Sodium.KeyExchange.Crypto_KX_Seed_KeyPair(seed.ToArray());
        return new X25519KeyPair(keyPair.PrivateKey, keyPair.PublicKey);
    }

    public override byte[] GeneratePublicKey(ReadOnlySpan<byte> privateKey)
    {
        return ScalarMultiplication.Crypto_ScalarMult_Base(privateKey.ToArray());
    }

    public override byte[] GenerateSharedKey(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
    {
        return ScalarMultiplication.Crypto_ScalarMult(privateKey.ToArray(), publicKey.ToArray());
    }
}
