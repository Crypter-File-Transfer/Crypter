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

using System.Runtime.Versioning;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.ConstantTime;
using Crypter.Crypto.Common.CryptoHash;
using Crypter.Crypto.Common.DigitalSignature;
using Crypter.Crypto.Common.Encryption;
using Crypter.Crypto.Common.GenericHash;
using Crypter.Crypto.Common.KeyExchange;
using Crypter.Crypto.Common.Padding;
using Crypter.Crypto.Common.PasswordHash;
using Crypter.Crypto.Common.Random;
using Crypter.Crypto.Common.StreamEncryption;
using Crypter.Crypto.Common.StreamGenericHash;
using Crypter.Crypto.Providers.Default.Wrappers;
using Random = Crypter.Crypto.Providers.Default.Wrappers.Random;

namespace Crypter.Crypto.Providers.Default;

[UnsupportedOSPlatform("browser")]
public class DefaultCryptoProvider : ICryptoProvider
{
    public IConstantTime ConstantTime { get; init; }
    public ICryptoHash CryptoHash { get; init; }
    public IDigitalSignature DigitalSignature { get; init; }
    public IEncryption Encryption { get; init; }
    public IGenericHash GenericHash { get; init; }
    public IKeyExchange KeyExchange { get; init; }
    public IPadding Padding { get; init; }
    public IPasswordHash PasswordHash { get; init; }
    public IRandom Random { get; init; }
    public IStreamEncryptionFactory StreamEncryptionFactory { get; init; }
    public IStreamGenericHashFactory StreamGenericHashFactory { get; init; }

    public DefaultCryptoProvider()
    {
        ConstantTime = new ConstantTime();
        CryptoHash = new CryptoHash();
        DigitalSignature = new DigitalSignature();
        Encryption = new Encryption();
        GenericHash = new GenericHash();
        Padding = new Padding();
        PasswordHash = new PasswordHash();
        Random = new Random();
        StreamEncryptionFactory = new StreamEncryptionFactory(Padding);
        StreamGenericHashFactory = new StreamGenericHashFactory();

        KeyExchange = new KeyExchange(StreamGenericHashFactory);
    }
}
