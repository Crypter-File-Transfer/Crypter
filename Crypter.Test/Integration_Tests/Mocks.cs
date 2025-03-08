/*
 * Copyright (C) 2024 Crypter File Transfer
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

using Crypter.Crypto.Common;
using Crypter.Crypto.Common.CryptoHash;
using Crypter.Crypto.Common.DigitalSignature;
using Crypter.Crypto.Providers.Default.Wrappers;
using NSubstitute;
using Random = Crypter.Crypto.Providers.Default.Wrappers.Random;

namespace Crypter.Test.Integration_Tests;

internal static class Mocks
{
    internal static ICryptoProvider CreateDeterministicCryptoProvider(Ed25519KeyPair keyPairToReturn)
    {
        ICryptoProvider cryptoProviderMock = Substitute.For<ICryptoProvider>();
        cryptoProviderMock.ConstantTime.Returns(new ConstantTime());
        cryptoProviderMock.CryptoHash.Returns(new CryptoHash());
        cryptoProviderMock.GenericHash.Returns(new GenericHash());
        cryptoProviderMock.Padding.Returns(new Padding());
        cryptoProviderMock.Random.Returns(new Random());

        DigitalSignature digitalSignatureMock = Substitute.ForPartsOf<DigitalSignature>();
        digitalSignatureMock.GenerateKeyPair().Returns(keyPairToReturn);

        cryptoProviderMock.DigitalSignature.Returns(digitalSignatureMock);
        return cryptoProviderMock;
    }
}
