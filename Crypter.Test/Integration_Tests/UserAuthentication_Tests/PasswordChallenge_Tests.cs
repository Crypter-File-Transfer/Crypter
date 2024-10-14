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

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserAuthentication_Tests;

[TestFixture]
internal class PasswordChallenge_Tests
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
    public async Task Password_Challenge_Works()
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> registrationResult = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        PasswordChallengeRequest request = new PasswordChallengeRequest(registrationRequest.VersionedPassword.Password);
        Either<PasswordChallengeError, Unit> result = await _client!.UserAuthentication.PasswordChallengeAsync(request);

        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);
        Assert.That(result.IsRight, Is.True);
    }

    [Test]
    public async Task Password_Challenge_Fails_Not_Authenticated()
    {
        RegistrationRequest registrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> registrationResult = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        PasswordChallengeRequest request = new PasswordChallengeRequest(registrationRequest.VersionedPassword.Password);
        using HttpRequestMessage requestMessage =
            new HttpRequestMessage(HttpMethod.Post, "api/user/authentication/password/challenge");
        requestMessage.Content = JsonContent.Create(request);
        using HttpClient rawClient = _factory!.CreateClient();
        HttpResponseMessage response = await rawClient.SendAsync(requestMessage);

        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Password_Challenge_Fails_Wrong_Password()
    {
        RegistrationRequest registrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> registrationResult = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest =
            TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        byte[] wrongPassword = new byte[registrationRequest.VersionedPassword.Password.Length];
        registrationRequest.VersionedPassword.Password.CopyTo(wrongPassword, 0);
        wrongPassword[0] = (byte)(wrongPassword[0] == 0x01
            ? 0x02
            : 0x01);

        PasswordChallengeRequest request = new PasswordChallengeRequest(wrongPassword);
        Either<PasswordChallengeError, Unit> result = await _client!.UserAuthentication.PasswordChallengeAsync(request);

        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);
        Assert.That(registrationRequest.VersionedPassword.Password, Is.Not.EqualTo(wrongPassword));
        Assert.That(result.IsLeft, Is.True);
    }
}
