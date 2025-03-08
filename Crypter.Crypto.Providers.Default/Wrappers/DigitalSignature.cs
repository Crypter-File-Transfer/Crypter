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
using Crypter.Crypto.Common.DigitalSignature;
using Geralt;

namespace Crypter.Crypto.Providers.Default.Wrappers;

[UnsupportedOSPlatform("browser")]
public class DigitalSignature : IDigitalSignature
{
    public virtual Ed25519KeyPair GenerateKeyPair()
    {
        byte[] privateKey = new byte[Ed25519.PrivateKeySize];
        byte[] publicKey = new byte[Ed25519.PublicKeySize];
        Ed25519.GenerateKeyPair(publicKey, privateKey);
        return new Ed25519KeyPair(privateKey, publicKey);
    }

    public Ed25519KeyPair GenerateKeyPair(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != Ed25519.SeedSize)
        {
            throw new ArgumentOutOfRangeException(nameof(seed), "Seed must be of length Ed25519.SeedSize");
        }
        byte[] privateKey = new byte[Ed25519.PrivateKeySize];
        byte[] publicKey = new byte[Ed25519.PublicKeySize];
        
        Ed25519.GenerateKeyPair(publicKey, privateKey, seed);
        return new Ed25519KeyPair(privateKey, publicKey);
    }

    public byte[] GenerateSignature(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> message)
    {
        byte[] signature = new byte[Ed25519.SignatureSize];
        Ed25519.Sign(signature, message, privateKey);
        return signature;
    }

    public bool VerifySignature(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        return Ed25519.Verify(signature, message, publicKey);
    }
}
