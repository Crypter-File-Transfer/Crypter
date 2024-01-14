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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.UserAuthentication;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserAuthentication_Tests;

[TestFixture]
internal class Registration_Tests
{
    private WebApplicationFactory<Program> _factory;
    private ICrypterApiClient _client;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        (_client, _) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
        await AssemblySetup.InitializeRespawnerAsync();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        await _factory.DisposeAsync();
        await AssemblySetup.ResetServerDataAsync();
    }

    [TestCase(TestData.DefaultEmailAdress)]
    [TestCase(null)]
    public async Task Register_User_Works(string emailAddress)
    {
        RegistrationRequest request =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword, emailAddress);
        Either<RegistrationError, Unit> result = await _client.UserAuthentication.RegisterAsync(request);

        Assert.That(result.IsRight, Is.True);
    }

    [TestCase("FOO", "foo")]
    [TestCase("foo", "FOO")]
    public async Task Register_User_Fails_For_Duplicate_Username(string initialUsername, string duplicateUsername)
    {
        VersionedPassword password = new VersionedPassword("password"u8.ToArray(), 1);

        RegistrationRequest initialRequest = new RegistrationRequest(initialUsername, password);
        Either<RegistrationError, Unit> initialResult = await _client.UserAuthentication.RegisterAsync(initialRequest);

        RegistrationRequest secondRequest = new RegistrationRequest(duplicateUsername, password);
        Either<RegistrationError, Unit> secondResult = await _client.UserAuthentication.RegisterAsync(secondRequest);

        Assert.That(initialResult.IsRight, Is.True);
        Assert.That(secondResult.IsLeft, Is.True);
    }

    [TestCase("FOO@foo.com", "foo@foo.com")]
    [TestCase("foo@foo.com", "FOO@foo.com")]
    public async Task Register_User_Fails_For_Duplicate_Email_Address(string initialEmailAddress,
        string duplicateEmailAddress)
    {
        VersionedPassword password = new VersionedPassword("password"u8.ToArray(), 1);

        RegistrationRequest initialRequest = new RegistrationRequest("first", password, initialEmailAddress);
        Either<RegistrationError, Unit> initialResult = await _client.UserAuthentication.RegisterAsync(initialRequest);

        RegistrationRequest secondRequest = new RegistrationRequest("second", password, duplicateEmailAddress);
        Either<RegistrationError, Unit> secondResult = await _client.UserAuthentication.RegisterAsync(secondRequest);

        Assert.That(initialResult.IsRight, Is.True);
        Assert.That(secondResult.IsLeft, Is.True);
    }
}
