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

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Client.Services;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserRecovery.RequestRecovery;
using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Common.Enums;
using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Core.Services;
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
using UserRecoveryService = Crypter.Common.Client.Services.UserRecoveryService;

namespace Crypter.Test.Integration_Tests.UserRecovery_Tests;

[TestFixture]
internal class SubmitRecovery_Tests
{
    private WebApplicationFactory<Program> _factory;
    private ICrypterApiClient _client;
    private ITokenRepository _clientTokenRepository;

    private ICryptoProvider _mockCryptoProvider;
    private DefaultCryptoProvider _cryptoProvider;
    private Ed25519KeyPair _knownKeyPair;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        _cryptoProvider = new DefaultCryptoProvider();
        _knownKeyPair = _cryptoProvider.DigitalSignature.GenerateKeyPair();
        _mockCryptoProvider = Mocks.CreateDeterministicCryptoProvider(_knownKeyPair).Object;
    }

    [SetUp]
    public async Task SetupTestAsync()
    {
        IServiceCollection overrideServices = new ServiceCollection();
        overrideServices.AddSingleton(_mockCryptoProvider);

        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync(true, overrideServices);
        (_client, _clientTokenRepository) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        await _factory.DisposeAsync();
        await AssemblySetup.ResetServerDataAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Submit_Recovery_Works_Async(bool withRecoveryProof)
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername,
            TestData.DefaultPassword, TestData.DefaultEmailAdress);
        Either<RegistrationError, Unit> registrationResult =
            await _client.UserAuthentication.RegisterAsync(registrationRequest);

        // Allow the background service to "send" the verification email and save the email verification data
        await Task.Delay(5000);

        Maybe<RecoveryKey> recoveryKey = Maybe<RecoveryKey>.None;
        if (withRecoveryProof)
        {
            LoginRequest loginRequest =
                TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword, TokenType.Session);
            var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

            await loginResult.DoRightAsync(async loginResponse =>
            {
                await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
                await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
            });

            (byte[] masterKey, InsertMasterKeyRequest insertMasterKeyRequest) =
                TestData.GetInsertMasterKeyRequest(TestData.DefaultPassword);
            Either<InsertMasterKeyError, Unit> insertMasterKeyResponse =
                await _client.UserKey.InsertMasterKeyAsync(insertMasterKeyRequest);

            UserPasswordService userPasswordService = new UserPasswordService(_cryptoProvider);
            UserRecoveryService userRecoveryService =
                new UserRecoveryService(_client, new DefaultCryptoProvider(), userPasswordService);
            recoveryKey = await userRecoveryService.DeriveRecoveryKeyAsync(masterKey,
                Username.From(TestData.DefaultUsername), registrationRequest.VersionedPassword);
            recoveryKey.IfNone(Assert.Fail);
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        DataContext dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        UserEmailVerificationEntity verificationData = await dataContext.UserEmailVerifications
            .Where(x => x.User.Username == TestData.DefaultUsername)
            .FirstAsync();

        string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(verificationData.Code);
        byte[] signedVerificationCode =
            _cryptoProvider.DigitalSignature.GenerateSignature(_knownKeyPair.PrivateKey,
                verificationData.Code.ToByteArray());
        string encodedVerificationSignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedVerificationCode);

        VerifyEmailAddressRequest verificationRequest =
            new VerifyEmailAddressRequest(encodedVerificationCode, encodedVerificationSignature);
        Either<VerifyEmailAddressError, Unit> verificationResult =
            await _client.UserSetting.VerifyUserEmailAddressAsync(verificationRequest);

        EmailAddress emailAddress = EmailAddress.From(TestData.DefaultEmailAdress);
        Either<SendRecoveryEmailError, Unit> sendRecoveryEmailResult =
            await _client.UserRecovery.SendRecoveryEmailAsync(emailAddress);

        // Allow the background service to "send" the recovery email and save the recovery data
        await Task.Delay(5000);

        UserRecoveryEntity recoveryData = await dataContext.UserRecoveries
            .Where(x => x.User.Username == TestData.DefaultUsername)
            .FirstAsync();

        IUserRecoveryService recoveryService = _factory.Services.GetRequiredService<IUserRecoveryService>();
        string encodedRecoveryCode = UrlSafeEncoder.EncodeGuidUrlSafe(recoveryData.Code);
        byte[] signedRecoveryData = recoveryService.GenerateRecoverySignature(_knownKeyPair.PrivateKey,
            recoveryData.Code, Username.From(TestData.DefaultUsername));
        string encodedRecoverySignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedRecoveryData);

        VersionedPassword versionedPassword =
            new VersionedPassword(Encoding.UTF8.GetBytes(TestData.DefaultPassword), 1);

        ReplacementMasterKeyInformation replacementMasterKeyInformation = recoveryKey.Match(
            () => null,
            x => new ReplacementMasterKeyInformation(x.Proof, new byte[] { 0x01 }, new byte[] { 0x02 },
                new byte[] { 0x03 }));
        SubmitRecoveryRequest request = new SubmitRecoveryRequest(TestData.DefaultUsername, encodedRecoveryCode,
            encodedRecoverySignature, versionedPassword, replacementMasterKeyInformation);
        Either<SubmitRecoveryError, Unit> result = await _client.UserRecovery.SubmitRecoveryAsync(request);

        Assert.True(registrationResult.IsRight);
        Assert.True(verificationResult.IsRight);
        Assert.True(sendRecoveryEmailResult.IsRight);
        Assert.True(verificationResult.IsRight);
        Assert.True(result.IsRight);
    }

    [TestCase(0)]
    [TestCase(1)]
    public async Task Submit_Recovery_Fails_When_Recovery_Code_Provided_Without_New_Master_Key_Information(
        int failureCase)
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername,
            TestData.DefaultPassword, TestData.DefaultEmailAdress);
        Either<RegistrationError, Unit> registrationResult =
            await _client.UserAuthentication.RegisterAsync(registrationRequest);

        // Allow the background service to "send" the verification email and save the email verification data
        await Task.Delay(5000);

        using IServiceScope scope = _factory.Services.CreateScope();
        DataContext dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        UserEmailVerificationEntity verificationData = await dataContext.UserEmailVerifications
            .Where(x => x.User.Username == TestData.DefaultUsername)
            .FirstAsync();

        string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(verificationData.Code);
        byte[] signedVerificationCode =
            _cryptoProvider.DigitalSignature.GenerateSignature(_knownKeyPair.PrivateKey,
                verificationData.Code.ToByteArray());
        string encodedVerificationSignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedVerificationCode);

        VerifyEmailAddressRequest verificationRequest =
            new VerifyEmailAddressRequest(encodedVerificationCode, encodedVerificationSignature);
        Either<VerifyEmailAddressError, Unit> verificationResult =
            await _client.UserSetting.VerifyUserEmailAddressAsync(verificationRequest);

        EmailAddress emailAddress = EmailAddress.From(TestData.DefaultEmailAdress);
        Either<SendRecoveryEmailError, Unit> sendRecoveryEmailResult =
            await _client.UserRecovery.SendRecoveryEmailAsync(emailAddress);

        // Allow the background service to "send" the recovery email and save the recovery data
        await Task.Delay(5000);

        LoginRequest loginRequest =
            TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword, TokenType.Session);
        var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        (byte[] masterKey, InsertMasterKeyRequest insertMasterKeyRequest) =
            TestData.GetInsertMasterKeyRequest(TestData.DefaultPassword);
        Either<InsertMasterKeyError, Unit> insertMasterKeyResponse =
            await _client.UserKey.InsertMasterKeyAsync(insertMasterKeyRequest);

        UserPasswordService userPasswordService = new UserPasswordService(_cryptoProvider);
        UserRecoveryService userRecoveryService =
            new UserRecoveryService(_client, new DefaultCryptoProvider(), userPasswordService);
        RecoveryKey recoveryKey = await userRecoveryService.DeriveRecoveryKeyAsync(masterKey,
                Username.From(TestData.DefaultUsername), registrationRequest.VersionedPassword)
            .SomeOrDefaultAsync(null);

        UserRecoveryEntity recoveryData = await dataContext.UserRecoveries
            .Where(x => x.User.Username == TestData.DefaultUsername)
            .FirstAsync();

        IUserRecoveryService recoveryService = _factory.Services.GetRequiredService<IUserRecoveryService>();
        string encodedRecoveryCode = UrlSafeEncoder.EncodeGuidUrlSafe(recoveryData.Code);
        byte[] signedRecoveryData = recoveryService.GenerateRecoverySignature(_knownKeyPair.PrivateKey,
            recoveryData.Code, Username.From(TestData.DefaultUsername));
        string encodedRecoverySignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedRecoveryData);

        VersionedPassword versionedPassword =
            new VersionedPassword(Encoding.UTF8.GetBytes(TestData.DefaultPassword), 1);

        ReplacementMasterKeyInformation replacementMasterKeyInformation = failureCase switch
        {
            0 => new ReplacementMasterKeyInformation(recoveryKey.Proof, null, null, null),
            1 => new ReplacementMasterKeyInformation(recoveryKey.Proof, Array.Empty<byte>(), Array.Empty<byte>(),
                Array.Empty<byte>()),
            _ => throw new NotImplementedException()
        };

        SubmitRecoveryRequest request = new SubmitRecoveryRequest(TestData.DefaultUsername, encodedRecoveryCode,
            encodedRecoverySignature, versionedPassword, replacementMasterKeyInformation);
        Either<SubmitRecoveryError, Unit> result = await _client.UserRecovery.SubmitRecoveryAsync(request);

        Assert.True(registrationResult.IsRight);
        Assert.True(verificationResult.IsRight);
        Assert.True(sendRecoveryEmailResult.IsRight);
        Assert.True(verificationResult.IsRight);
        Assert.True(result.IsLeft);
        result.DoLeftOrNeither(x =>
                Assert.AreEqual(SubmitRecoveryError.InvalidMasterKey, x),
            Assert.Fail);
    }
}
