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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserContact_Tests;

[TestFixture]
internal class AddUserContact_Tests
{
    private WebApplicationFactory<Program>? _factory;
    private ICrypterApiClient? _client;
    private ITokenRepository? _clientTokenRepository;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        (_client, _clientTokenRepository) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
        await AssemblySetup.InitializeRespawnerAsync();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Add_User_Contact_Works_Async()
    {
        const string contactUsername = "Samwise";
        const string contactPassword = "dropping_no_eaves";

        RegistrationRequest contactRegistrationRequest =
            TestData.GetRegistrationRequest(contactUsername, contactPassword);
        Either<RegistrationError, Unit> contactRegistrationResult = await _client!.UserAuthentication.RegisterAsync(contactRegistrationRequest);

        RegistrationRequest userRegistrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> userRegistrationResult = await _client!.UserAuthentication.RegisterAsync(userRegistrationRequest);

        LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> userLoginResult = await _client!.UserAuthentication.LoginAsync(userLoginRequest);

        await userLoginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        Either<AddUserContactError, UserContact> result = await _client!.UserContact.AddUserContactAsync(contactUsername);

        Assert.That(contactRegistrationResult.IsRight, Is.True);
        Assert.That(userRegistrationResult.IsRight, Is.True);
        Assert.That(userLoginResult.IsRight, Is.True);
        Assert.That(result.IsRight, Is.True);
    }

    [TestCase]
    public async Task Add_User_Contact_Fails_For_Same_User_Async()
    {
        RegistrationRequest userRegistrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> userRegistrationResult = await _client!.UserAuthentication.RegisterAsync(userRegistrationRequest);

        LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> userLoginResult = await _client!.UserAuthentication.LoginAsync(userLoginRequest);

        await userLoginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        Either<AddUserContactError, UserContact> result = await _client!.UserContact.AddUserContactAsync(TestData.DefaultUsername);

        Assert.That(userRegistrationResult.IsRight, Is.True);
        Assert.That(userLoginResult.IsRight, Is.True);
        Assert.That(result.IsLeft, Is.True);
    }

    [TestCase]
    public async Task Add_User_Contact_Fails_For_User_Not_Found_Async()
    {
        RegistrationRequest userRegistrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> userRegistrationResult = await _client!.UserAuthentication.RegisterAsync(userRegistrationRequest);

        LoginRequest userLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> userLoginResult = await _client!.UserAuthentication.LoginAsync(userLoginRequest);

        await userLoginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        Either<AddUserContactError, UserContact> result = await _client!.UserContact.AddUserContactAsync("Tom_Bombadil");

        Assert.That(userRegistrationResult.IsRight, Is.True);
        Assert.That(userLoginResult.IsRight, Is.True);
        Assert.That(result.IsLeft, Is.True);
    }
}
