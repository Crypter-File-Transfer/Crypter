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
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserAuthentication.PasswordChange;
using Crypter.Common.Enums;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserAuthentication_Tests;

[TestFixture]
internal class PasswordChange_Tests
{
    private WebApplicationFactory<Program>? _factory;
    private ICrypterApiClient? _client;
    private ITokenRepository? _clientTokenRepository;
    private IServiceScope? _scope;
    private DataContext? _dataContext;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        (_client, _clientTokenRepository) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
        await AssemblySetup.InitializeRespawnerAsync();
        
        _scope = _factory.Services.CreateScope();
        _dataContext = _scope.ServiceProvider.GetRequiredService<DataContext>();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        _scope?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Password_Change_Works()
    {
        const string updatedPassword = "new password";
        
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> registrationResult = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        List<VersionedPassword> oldPasswords = [TestData.GetVersionedPassword(TestData.DefaultPassword, 1)];
        VersionedPassword newPassword = TestData.GetVersionedPassword(updatedPassword, 1);
        PasswordChangeRequest request = new PasswordChangeRequest(oldPasswords, newPassword);
        Either<PasswordChangeError, Unit> result = await _client!.UserAuthentication.ChangePasswordAsync(request);

        LoginRequest newLoginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, updatedPassword);
        Either<LoginError, LoginResponse> newLoginResult = await _client!.UserAuthentication.LoginAsync(newLoginRequest);
        
        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);
        Assert.That(result.IsRight, Is.True);
        Assert.That(newLoginResult.IsRight, Is.True);
    }

    [Test]
    public async Task Password_Change_Implicitly_Upgrades_Client_Password_Version()
    {
        const string updatedPassword = "new password";
        
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        await _client!.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        UserEntity userEntity = await _dataContext!.Users
            .AsTracking()
            .Where(x => x.Username == TestData.DefaultUsername)
            .FirstAsync();

        userEntity.ClientPasswordVersion = 0;
        await _dataContext.SaveChangesAsync();
        
        List<VersionedPassword> oldPasswords = [TestData.GetVersionedPassword(TestData.DefaultPassword, 0)];
        VersionedPassword newPassword = TestData.GetVersionedPassword(updatedPassword, 1);
        PasswordChangeRequest request = new PasswordChangeRequest(oldPasswords, newPassword);
        await _client!.UserAuthentication.ChangePasswordAsync(request);

        IServiceScope newScope = _factory!.Services.CreateScope();
        DataContext newDataContext = newScope.ServiceProvider.GetRequiredService<DataContext>();
        
        UserEntity newUserEntity = await newDataContext.Users
            .Where(x => x.Username == TestData.DefaultUsername)
            .FirstAsync();

        Assert.That(newUserEntity.ClientPasswordVersion, Is.EqualTo(1));
    }

    [Test]
    public async Task Password_Change_Rejects_Incorrect_Old_Password()
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

        List<VersionedPassword> oldPasswords = [TestData.GetVersionedPassword("not the right password", 1)];
        VersionedPassword newPassword = TestData.GetVersionedPassword("new password", 1);
        PasswordChangeRequest request = new PasswordChangeRequest(oldPasswords, newPassword);
        Either<PasswordChangeError, Unit> result = await _client!.UserAuthentication.ChangePasswordAsync(request);
        
        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);
        Assert.That(result.IsLeft, Is.True);
    }

    [Test]
    public async Task Password_Change_Rejects_Incorrect_New_Password_Version()
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

        List<VersionedPassword> oldPasswords = [TestData.GetVersionedPassword(TestData.DefaultPassword, 0)];
        VersionedPassword newPassword = TestData.GetVersionedPassword("new password", 1);
        PasswordChangeRequest request = new PasswordChangeRequest(oldPasswords, newPassword);
        Either<PasswordChangeError, Unit> result = await _client!.UserAuthentication.ChangePasswordAsync(request);
        
        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);
        Assert.That(result.IsLeft, Is.True);
    }
}
