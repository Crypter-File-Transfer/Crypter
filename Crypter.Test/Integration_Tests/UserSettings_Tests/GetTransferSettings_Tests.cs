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

using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserSettings.TransferSettings;
using Crypter.Common.Enums;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserSettings_Tests;

public class GetTransferSettings_Tests
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
    public async Task Get_Anonymous_Transfer_Settings_Works()
    {
        Maybe<GetTransferSettingsResponse> settingsResponse = await _client!.UserSetting.GetTransferSettingsAsync(false);
        settingsResponse.IfSome(x =>
            Assert.Multiple(() =>
            {
                Assert.That(x.TierName, Is.EqualTo("Anonymous Users"));
                Assert.That(x.MaximumUploadSize, Is.EqualTo(1000000L));
                Assert.That(x.UserQuota, Is.EqualTo(1000000000L));
                Assert.That(x.FreeTransferQuota, Is.EqualTo(1000000000L));
                Assert.That(x.AvailableUserSpace, Is.EqualTo(x.UserQuota));
                Assert.That(x.AvailableFreeTransferSpace, Is.EqualTo(x.FreeTransferQuota));
            }))
        .IfNone(Assert.Fail);
    }

    [Test]
    public async Task Get_Authenticated_Transfer_Settings_Works()
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> _ = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });
        
        Maybe<GetTransferSettingsResponse> settingsResponse = await _client!.UserSetting.GetTransferSettingsAsync(true);
        settingsResponse.IfSome(x =>
                Assert.Multiple(() =>
                {
                    Assert.That(x.TierName, Is.EqualTo("Authenticated Users"));
                    Assert.That(x.MaximumUploadSize, Is.EqualTo(1000000L));
                    Assert.That(x.UserQuota, Is.EqualTo(1000000000L));
                    Assert.That(x.FreeTransferQuota, Is.EqualTo(1000000000L));
                    Assert.That(x.AvailableUserSpace, Is.EqualTo(x.UserQuota));
                    Assert.That(x.AvailableFreeTransferSpace, Is.EqualTo(x.FreeTransferQuota));
                }))
            .IfNone(Assert.Fail);
    }
    
    [Test]
    public async Task Get_Verified_Transfer_Settings_Works()
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> _ = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        DataContext dataContext = _factory!.Services.GetRequiredService<DataContext>();
        UserEntity userEntity = await dataContext.Users
            .Where(x => x.Username == TestData.DefaultUsername)
            .FirstAsync();
        userEntity.EmailAddress = "test@test.com";
        await dataContext.SaveChangesAsync();
        
        Maybe<GetTransferSettingsResponse> settingsResponse = await _client!.UserSetting.GetTransferSettingsAsync(true);
        settingsResponse.IfSome(x =>
                Assert.Multiple(() =>
                {
                    Assert.That(x.TierName, Is.EqualTo("Verified Users"));
                    Assert.That(x.MaximumUploadSize, Is.EqualTo(1000000L));
                    Assert.That(x.UserQuota, Is.EqualTo(1000000000L));
                    Assert.That(x.FreeTransferQuota, Is.EqualTo(1000000000L));
                    Assert.That(x.AvailableUserSpace, Is.EqualTo(x.UserQuota));
                    Assert.That(x.AvailableFreeTransferSpace, Is.EqualTo(x.FreeTransferQuota));
                }))
            .IfNone(Assert.Fail);
    }
}
