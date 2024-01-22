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

using System;
using System.Collections.Generic;
using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests;

[TestFixture]
public class PasswordHashService_Tests
{
    private PasswordHashService? _sut;

    [SetUp]
    public void Setup()
    {
        ICryptoProvider cryptoProvider = new DefaultCryptoProvider();

        ServerPasswordSettings settings = new ServerPasswordSettings
        {
            ClientVersion = 1,
            ServerVersions = new List<PasswordVersion>
            {
                new PasswordVersion { Version = 0, Algorithm = "PBKDF2", Iterations = 1 },
                new PasswordVersion { Version = 1, Algorithm = "PBKDF2", Iterations = 2 },
                new PasswordVersion { Version = 2, Algorithm = "PBKDF2", Iterations = 100001 },
            }
        };
        IOptions<ServerPasswordSettings> options = Options.Create(settings);
        _sut = new PasswordHashService(cryptoProvider, options);
    }

    [Test]
    public void Service_Exists_In_Crypter_Core()
    {
        Type assembly = typeof(PasswordHashService);
        Assert.That(assembly.FullName, Is.EqualTo("Crypter.Core.Services.PasswordHashService"));
    }

    [Test]
    public void Salt_Is_16_Bytes()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);
        Assert.That(hashOutput.Salt.Length, Is.EqualTo(16));
    }

    [Test]
    public void Hash_Is_64_Bytes()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);
        Assert.That(hashOutput.Hash.Length, Is.EqualTo(64));
    }

    [Test]
    public void Salts_Are_Unique()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput1 = _sut!.MakeSecurePasswordHash(password, 0);
        SecurePasswordHashOutput hashOutput2 = _sut.MakeSecurePasswordHash(password, 0);
        Assert.That(hashOutput1.Salt, Is.Not.EqualTo(hashOutput2.Salt));
    }

    [Test]
    public void Hashes_With_Unique_Salts_Are_Unique()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput1 = _sut!.MakeSecurePasswordHash(password, 0);
        SecurePasswordHashOutput hashOutput2 = _sut.MakeSecurePasswordHash(password, 0);
        Assert.That(hashOutput1.Hash, Is.Not.EqualTo(hashOutput2.Hash));
    }

    [Test]
    public void Hash_Verification_Can_Succeed()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);
        bool hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 0);
        Assert.That(hashesMatch, Is.True);
    }

    [Test]
    public void Hash_Verification_Fails_With_Bad_Password()
    {
        byte[] password = "foo"u8.ToArray();
        byte[] notPassword = "not foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);
        bool hashesMatch = _sut.VerifySecurePasswordHash(notPassword, hashOutput.Hash, hashOutput.Salt, 0);
        Assert.That(hashesMatch, Is.False);
    }

    [Test]
    public void Hash_Verification_Fails_With_Bad_Salt()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);

        // Modify the first byte in the salt to make it "bad"
        hashOutput.Salt[0] = hashOutput.Salt[0] == 0x01
            ? hashOutput.Salt[0] = 0x02
            : hashOutput.Salt[0] = 0x01;

        bool hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 0);
        Assert.That(hashesMatch, Is.False);
    }

    [Test]
    public void Hash_Verification_Fails_With_Different_Iterations()
    {
        byte[] password = "foo"u8.ToArray();
        SecurePasswordHashOutput hashOutput = _sut!.MakeSecurePasswordHash(password, 0);

        bool hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 1);
        Assert.That(hashesMatch, Is.False);
    }

    [Test]
    public void Hash_Verification_Is_Stable()
    {
        byte[] password = "foo"u8.ToArray();
        byte[] salt =
        [
            0xa1, 0xb8, 0x4d, 0x9b, 0x83, 0xf6, 0xb3, 0x46,
            0xa1, 0x85, 0x2a, 0xc6, 0xee, 0x28, 0x77, 0xe8
        ];

        byte[] hash =
        [
            0x6b, 0x8a, 0x8f, 0x2e, 0x8d, 0xa5, 0x50, 0x4e,
            0x86, 0xca, 0x8e, 0x62, 0x23, 0x47, 0x15, 0xb0,
            0x36, 0xdb, 0x34, 0xc1, 0xa7, 0x93, 0x1d, 0x47,
            0x3b, 0xc6, 0xca, 0xfa, 0x99, 0x88, 0xf8, 0xfb,
            0x96, 0x6c, 0x82, 0xa4, 0x6f, 0xd7, 0xc6, 0x03,
            0xf7, 0x52, 0xc5, 0xf4, 0x37, 0x5d, 0x34, 0x19,
            0x52, 0x7a, 0xf3, 0x2c, 0xe2, 0x52, 0x47, 0x06,
            0x90, 0xe8, 0x6b, 0x19, 0x70, 0x27, 0xa5, 0xc6
        ];

        bool hashesMatch = _sut!.VerifySecurePasswordHash(password, hash, salt, 2);
        Assert.That(hashesMatch, Is.True);
    }
}
