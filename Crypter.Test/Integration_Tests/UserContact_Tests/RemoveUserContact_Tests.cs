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

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserContact_Tests;

[TestFixture]
internal class RemoveUserContact_Tests
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
    public async Task Remove_User_Contact_Works_Async()
    {
        const string contactUsername = "Samwise";
        const string contactPassword = "dropping_no_eaves";

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

        Maybe<List<UserContact>> initialContactsResult = await _client!.UserContact.GetUserContactsAsync();

        RegistrationRequest contactRegistrationRequest =
            TestData.GetRegistrationRequest(contactUsername, contactPassword);
        Either<RegistrationError, Unit> contactRegistrationResult = await _client!.UserAuthentication.RegisterAsync(contactRegistrationRequest);

        Either<AddUserContactError, UserContact> addContactResult = await _client!.UserContact.AddUserContactAsync(contactUsername);
        Maybe<List<UserContact>> secondContactsResult = await _client!.UserContact.GetUserContactsAsync();

        Either<RemoveUserContactError, Unit> removeContactResult = await _client!.UserContact.RemoveUserContactAsync(contactUsername);
        Maybe<List<UserContact>> finalContactsResult = await _client!.UserContact.GetUserContactsAsync();

        Assert.That(userRegistrationResult.IsRight, Is.True);
        Assert.That(userLoginResult.IsRight, Is.True);
        Assert.That(initialContactsResult.IsSome, Is.True);
        initialContactsResult.IfSome(x => Assert.That(x, Is.Empty));

        Assert.That(contactRegistrationResult.IsRight, Is.True);
        Assert.That(addContactResult.IsRight, Is.True);
        Assert.That(secondContactsResult.IsSome, Is.True);
        secondContactsResult.IfSome(x =>
        {
            Assert.That(x.Count, Is.EqualTo(1));
            Assert.That(x[0].Username, Is.EqualTo(contactUsername));
        });

        Assert.That(removeContactResult.IsRight, Is.True);
        Assert.That(finalContactsResult.IsSome, Is.True);
        finalContactsResult.IfSome(x => Assert.That(x, Is.Empty));
    }

    [Test]
    public async Task Remove_User_Contact_Fails_For_Absent_Username_Parameter_Async()
    {
        await TestMethods.LoginAsync(_client!, _clientTokenRepository!);
        using HttpClient httpClient =
            await TestMethods.CreateAuthenticatedHttpClientAsync(_factory!, _clientTokenRepository!);

        using HttpResponseMessage response = await httpClient.DeleteAsync("api/user/contact");
        ErrorResponse? errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Errors, Has.Count.EqualTo(1));
        Assert.That(errorResponse.Errors[0].ErrorCode, Is.EqualTo((int)RemoveUserContactError.InvalidUser));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("no spaces allowed")]
    [TestCase("bad*characters")]
    public async Task Remove_User_Contact_Fails_For_Invalid_Username_Async(string contactUsername)
    {
        await TestMethods.LoginAsync(_client!, _clientTokenRepository!);

        Either<RemoveUserContactError, Unit> result = await _client!.UserContact.RemoveUserContactAsync(contactUsername);

        Assert.That(result.IsLeft, Is.True);
        result.DoLeftOrNeither(
            left: error => Assert.That(error, Is.EqualTo(RemoveUserContactError.InvalidUser)),
            neither: Assert.Fail);
    }

    [Test]
    public async Task Remove_User_Contact_Succeeds_For_Absent_Contact_Async()
    {
        await TestMethods.LoginAsync(_client!, _clientTokenRepository!);

        Either<RemoveUserContactError, Unit> result = await _client!.UserContact.RemoveUserContactAsync("Tom_Bombadil");

        Assert.That(result.IsRight, Is.True);
    }
}
