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
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.MessageTransfer_Tests;

[TestFixture]
internal class UploadMessageTransfer_Tests
{
    private WebApplicationFactory<Program> _factory;
    private ICrypterApiClient _client;
    private ITokenRepository _clientTokenRepository;

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
        await _factory.DisposeAsync();
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Upload_Anonymous_Message_Transfer_Works()
    {
        (Func<EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject,
            TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof,
            TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> result =
            await _client.MessageTransfer.UploadMessageTransferAsync(Maybe<string>.None, request,
                encryptionStreamOpener, false);

        Assert.That(result.IsRight, Is.True);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Upload_User_Message_Transfer_Works(bool senderDefined, bool recipientDefined)
    {
        Maybe<string> senderUsername = senderDefined
            ? TestData.DefaultUsername
            : Maybe<string>.None;
        const string senderPassword = TestData.DefaultPassword;

        Maybe<string> recipientUsername = recipientDefined
            ? "Samwise"
            : Maybe<string>.None;
        const string recipientPassword = "dropping_eaves";

        Assert.That((senderDefined == false && senderUsername.IsNone)
                    || (senderDefined && senderUsername.IsSome), Is.True);

        await senderUsername.IfSomeAsync(async username =>
        {
            RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(username, senderPassword);
            Either<RegistrationError, Unit> registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

            LoginRequest loginRequest = TestData.GetLoginRequest(username, senderPassword);
            Either<LoginError, LoginResponse> loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

            await loginResult.DoRightAsync(async loginResponse =>
            {
                await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
                await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
            });

            Assert.That(registrationResult.IsRight, Is.True);
            Assert.That(loginResult.IsRight, Is.True);
        });

        Assert.That((recipientDefined == false && recipientUsername.IsNone)
                    || (recipientDefined && recipientUsername.IsSome), Is.True);

        await recipientUsername.IfSomeAsync(async username =>
        {
            RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(username, recipientPassword);
            Either<RegistrationError, Unit> _ = await _client.UserAuthentication.RegisterAsync(registrationRequest);
        });

        (Func<EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject,
            TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof,
            TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> result = await _client.MessageTransfer.UploadMessageTransferAsync(recipientUsername, request,
            encryptionStreamOpener, senderDefined);

        Assert.That(result.IsRight, Is.True);
    }

    [Test]
    public async Task Upload_User_Message_Transfer_Fails_When_Recipient_Does_Not_Exist()
    {
        (Func<EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject,
            TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof,
            TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> result =
            await _client.MessageTransfer.UploadMessageTransferAsync("John Smith", request, encryptionStreamOpener,
                false);

        Assert.That(result.IsLeft, Is.True);
    }

    [Test]
    public void Upload_Authenticated_Message_Transfer_Throws_When_Not_Authenticated()
    {
        (Func<EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject,
            TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof,
            TestData.DefaultTransferLifetimeHours);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _client.MessageTransfer.UploadMessageTransferAsync(Maybe<string>.None, request,
                encryptionStreamOpener, true));
    }

    [Test]
    public async Task Upload_User_Message_Transfer_Works_After_Refresh_Occurs()
    {
        const string senderUsername = TestData.DefaultUsername;
        const string senderPassword = TestData.DefaultPassword;

        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(senderUsername, senderPassword);
        Either<RegistrationError, Unit> registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

        LoginRequest loginRequest = TestData.GetLoginRequest(senderUsername, senderPassword);
        Either<LoginError, LoginResponse> loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await _clientTokenRepository.StoreAuthenticationTokenAsync("bogus auth token");
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });

        Assert.That(registrationResult.IsRight, Is.True);
        Assert.That(loginResult.IsRight, Is.True);

        (Func<EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject,
            TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof,
            TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> result =
            await _client.MessageTransfer.UploadMessageTransferAsync(Maybe<string>.None, request,
                encryptionStreamOpener, true);

        Assert.That(result.IsRight, Is.True);
    }
}
