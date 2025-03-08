/*
 * Copyright (C) 2025 Crypter File Transfer
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
using Crypter.Core.Identity.Tokens;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using NUnit.Framework;
using System.Linq;
using System.Text;
using Geralt;

namespace Crypter.Test.Core_Tests.Identity_Tests;

[TestFixture]
public class EdDsa_Tests
{
    [Test]
    public void Can_Generate_And_Verify_EdDsa_Signature()
    {
        const string signableString = "Crypter.dev";
        byte[] toSignBytes = Encoding.UTF8.GetBytes(signableString);

        ICryptoProvider cryptoProvider = new DefaultCryptoProvider();
        EdDsaAlgorithm alg = new EdDsaAlgorithm(cryptoProvider);
        Assert.That(alg, Is.Not.Null);

        byte[] signature = alg.Sign(toSignBytes);

        Assert.That(alg.Verify(toSignBytes, signature));
    }

    [Test]
    public void Seeded_Key_Pair_Generation_Is_Deterministic()
    {
        const string encodedSeed = "9hBvkx3TqqL5rBYOZ51FnmNFeuFz9DmyY0/odnw9Z5Y=";
        byte[] seedSpan = Convert.FromBase64String(encodedSeed);

        byte[] privateKey1 = new byte[Ed25519.PrivateKeySize];
        byte[] publicKey1 = new byte[Ed25519.PublicKeySize];
        Ed25519.GenerateKeyPair(publicKey1, privateKey1, seedSpan);

        byte[] privateKey2 = new byte[Ed25519.PrivateKeySize];
        byte[] publicKey2 = new byte[Ed25519.PublicKeySize];
        Ed25519.GenerateKeyPair(publicKey2, privateKey2, seedSpan);

        Assert.That(privateKey1.SequenceEqual(privateKey2));
    }
}
