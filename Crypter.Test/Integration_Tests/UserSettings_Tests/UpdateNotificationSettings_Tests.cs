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

using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using Crypter.Common.Enums;
using Crypter.Common.Infrastructure;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.DigitalSignature;
using Crypter.Crypto.Providers.Default;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserSettings_Tests;

[TestFixture]
internal class UpdateNotificationSettings_Tests
{
    private WebApplicationFactory<Program>? _factory;
    private ICrypterApiClient? _client;
    private ITokenRepository? _clientTokenRepository;

    private DefaultCryptoProvider? _cryptoProvider;
    private Ed25519KeyPair? _emailVerificationKeyPair;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _cryptoProvider = new DefaultCryptoProvider();
        _emailVerificationKeyPair = _cryptoProvider.DigitalSignature.GenerateKeyPair();
        ICryptoProvider mockCryptoProvider = Mocks.CreateDeterministicCryptoProvider(_emailVerificationKeyPair);
        IServiceCollection overrideServices = new ServiceCollection();
        overrideServices.AddSingleton(mockCryptoProvider);

        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync(true, overrideServices);
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
    public async Task Update_Notification_Preferences_Works_Async()
    {
        await RegisterAndLoginAsync();

        NotificationSettings request = new NotificationSettings(false, false);
        Either<UpdateNotificationSettingsError, NotificationSettings> result =
            await _client!.UserSetting.UpdateNotificationSettingsAsync(request);

        Assert.That(result.IsRight, Is.True);
    }

    [Test]
    public async Task Update_Notification_Preferences_Works_For_Verified_Email_Address_Async()
    {
        await RegisterAndLoginAsync(TestData.DefaultEmailAdress);
        await VerifyEmailAddressAsync();

        NotificationSettings request = new NotificationSettings(true, true);
        Either<UpdateNotificationSettingsError, NotificationSettings> result =
            await _client!.UserSetting.UpdateNotificationSettingsAsync(request);

        Assert.That(result.IsRight, Is.True);
        result.DoRight(settings =>
        {
            Assert.That(settings.EmailNotifications, Is.True);
            Assert.That(settings.NotifyOnTransferReceived, Is.True);
        });
    }

    [Test]
    public async Task Update_Notification_Preferences_Fails_Without_Email_Address_Async()
    {
        await RegisterAndLoginAsync();

        NotificationSettings request = new NotificationSettings(true, true);
        Either<UpdateNotificationSettingsError, NotificationSettings> result =
            await _client!.UserSetting.UpdateNotificationSettingsAsync(request);

        Assert.That(result.IsLeft, Is.True);
        Assert.That(result.LeftOrDefault(UpdateNotificationSettingsError.UnknownError),
            Is.EqualTo(UpdateNotificationSettingsError.MissingNotificationChannel));
    }

    [Test]
    public async Task Update_Notification_Preferences_Fails_For_Unverified_Email_Address_Async()
    {
        await RegisterAndLoginAsync(TestData.DefaultEmailAdress);

        NotificationSettings request = new NotificationSettings(true, true);
        Either<UpdateNotificationSettingsError, NotificationSettings> result =
            await _client!.UserSetting.UpdateNotificationSettingsAsync(request);

        Assert.That(result.IsLeft, Is.True);
        Assert.That(result.LeftOrDefault(UpdateNotificationSettingsError.UnknownError),
            Is.EqualTo(UpdateNotificationSettingsError.MissingNotificationChannel));
    }

    [Test]
    public async Task Disable_Notification_Preferences_Works_Without_Email_Address_Async()
    {
        await RegisterAndLoginAsync();

        NotificationSettings request = new NotificationSettings(false, false);
        Either<UpdateNotificationSettingsError, NotificationSettings> result =
            await _client!.UserSetting.UpdateNotificationSettingsAsync(request);

        Assert.That(result.IsRight, Is.True);
        result.DoRight(settings =>
        {
            Assert.That(settings.EmailNotifications, Is.False);
            Assert.That(settings.NotifyOnTransferReceived, Is.False);
        });
    }

    private async Task RegisterAndLoginAsync(string? emailAddress = null)
    {
        RegistrationRequest registrationRequest =
            TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword, emailAddress);
        Either<RegistrationError, Unit> registrationResult =
            await _client!.UserAuthentication.RegisterAsync(registrationRequest);
        Assert.That(registrationResult.IsRight, Is.True);

        LoginRequest loginRequest =
            TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);
        Assert.That(loginResult.IsRight, Is.True);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });
    }

    private async Task VerifyEmailAddressAsync()
    {
        // Allow the background service to "send" the verification email and save the email verification data
        await Task.Delay(5000);

        using IServiceScope scope = _factory!.Services.CreateScope();
        DataContext dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        UserEmailChangeEntity changeData = await dataContext.UserEmailChangeRequests
            .Where(x => x.User!.Username == TestData.DefaultUsername)
            .FirstAsync();

        string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(changeData.Code!.Value);
        byte[] signedVerificationCode = _cryptoProvider!.DigitalSignature.GenerateSignature(
            _emailVerificationKeyPair!.PrivateKey, changeData.Code.Value.ToByteArray());
        string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedVerificationCode);

        VerifyEmailAddressRequest request = new VerifyEmailAddressRequest(encodedVerificationCode, encodedSignature);
        Either<VerifyEmailAddressError, Unit> result = await _client!.UserSetting.VerifyUserEmailAddressAsync(request);
        Assert.That(result.IsRight, Is.True);
    }
}
