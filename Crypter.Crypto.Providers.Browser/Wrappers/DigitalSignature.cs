﻿/*
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
using Crypter.Crypto.Common.DigitalSignature;

namespace Crypter.Crypto.Providers.Browser.Wrappers;

[SupportedOSPlatform("browser")]
public class DigitalSignature : IDigitalSignature
{
    public Ed25519KeyPair GenerateKeyPair()
    {
        BlazorSodium.Sodium.Models.Ed25519KeyPair keyPair = PublicKeySignature.Crypto_Sign_KeyPair();
        return new Ed25519KeyPair(keyPair.PrivateKey, keyPair.PublicKey);
    }

    public Ed25519KeyPair GenerateKeyPair(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != PublicKeySignature.SEED_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(seed));
        }
        BlazorSodium.Sodium.Models.Ed25519KeyPair keyPair = PublicKeySignature.Crypto_Sign_Seed_KeyPair(seed.ToArray());
        return new Ed25519KeyPair(keyPair.PrivateKey, keyPair.PublicKey);
    }

    public byte[] GenerateSignature(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> message)
    {
        return PublicKeySignature.Crypto_Sign_Detached(message.ToArray(), privateKey.ToArray());
    }

    public bool VerifySignature(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        return PublicKeySignature.Crypto_Sign_Verify_Detached(signature.ToArray(), message.ToArray(),
            publicKey.ToArray());
    }
}
