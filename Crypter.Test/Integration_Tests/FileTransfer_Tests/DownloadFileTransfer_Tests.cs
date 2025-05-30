﻿/*
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
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.FileTransfer_Tests;

[TestFixture]
internal class DownloadFileTransfer_Tests
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
    public async Task Download_Anonymous_File_Transfer_Works()
    {
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) = TestData.GetDefaultEncryptionStream();
        UploadFileTransferRequest uploadRequest = new UploadFileTransferRequest(TestData.DefaultTransferFileName,
            TestData.DefaultTransferFileContentType, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce,
            keyExchangeProof, TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> uploadResult = await _client!.FileTransfer.UploadFileTransferAsync(Maybe<string>.None, uploadRequest, encryptionStreamOpener,false);

        await uploadResult
            .DoRightAsync(async uploadResponse =>
            {
                Either<DownloadTransferCiphertextError, StreamDownloadResponse> result = await _client!.FileTransfer.GetAnonymousFileCiphertextAsync(uploadResponse.HashId, keyExchangeProof);
                
                Assert.That(uploadResult.IsRight, Is.True);
                Assert.That(result.IsRight, Is.True);
                result.DoRight(x => { Assert.DoesNotThrow(() => x.Stream.ReadByte()); });
            })
            .DoLeftOrNeitherAsync(Assert.Fail);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Download_User_File_Transfer_Works(bool senderDefined, bool recipientDefined)
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
            Either<RegistrationError, Unit> registrationResult = await _client!.UserAuthentication.RegisterAsync(registrationRequest);

            LoginRequest loginRequest = TestData.GetLoginRequest(username, senderPassword);
            Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

            await loginResult.DoRightAsync(async loginResponse =>
            {
                await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
                await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
            });

            Assert.That(registrationResult.IsRight, Is.True);
            Assert.That(loginResult.IsRight, Is.True);
        });

        Assert.That((recipientDefined == false && recipientUsername.IsNone)
                    || (recipientDefined && recipientUsername.IsSome), Is.True);

        await recipientUsername.IfSomeAsync(async username =>
        {
            RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(username, recipientPassword);
            Either<RegistrationError, Unit> _ = await _client!.UserAuthentication.RegisterAsync(registrationRequest);
        });

        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, byte[] keyExchangeProof) =
            TestData.GetDefaultEncryptionStream();
        UploadFileTransferRequest uploadRequest = new UploadFileTransferRequest(TestData.DefaultTransferFileName,
            TestData.DefaultTransferFileContentType, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce,
            keyExchangeProof, TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, UploadTransferResponse> uploadResult = await _client!.FileTransfer.UploadFileTransferAsync(recipientUsername, uploadRequest,
            encryptionStreamOpener, senderDefined);

        await recipientUsername.IfSomeAsync(async username =>
        {
            LoginRequest loginRequest = TestData.GetLoginRequest(username, recipientPassword);
            Either<LoginError, LoginResponse> loginResult = await _client!.UserAuthentication.LoginAsync(loginRequest);

            await loginResult.DoRightAsync(async loginResponse =>
            {
                await _clientTokenRepository!.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
                await _clientTokenRepository!.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
            });
        });
        
        await uploadResult
            .DoRightAsync(async uploadResponse =>
            {
                Either<DownloadTransferCiphertextError, StreamDownloadResponse> result =
                    await _client!.FileTransfer.GetUserFileCiphertextAsync(uploadResponse.HashId, keyExchangeProof, recipientDefined);

                Assert.That(uploadResult.IsRight, Is.True);
                Assert.That(result.IsRight, Is.True);
                result.DoRight(x => { Assert.DoesNotThrow(() => x.Stream.ReadByte()); });
            })
            .DoLeftOrNeitherAsync(Assert.Fail);
    }

    [Test]
    public async Task Download_Multipart_File_Transfer_Works()
    {
        string hashId = await TestMethods.InitiateMultipartFileTransferAsync(_client!, _clientTokenRepository!);
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, byte[] proof) = TestData.GetDefaultEncryptionStream();
        
        await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, 0, () => encryptionStreamOpener(null));
        await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, 1, () => encryptionStreamOpener(null));
        await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, 2, () => encryptionStreamOpener(null));
        await _client!.FileTransfer.FinalizeMultipartFileTransferAsync(hashId);
        
        Either<DownloadTransferCiphertextError, StreamDownloadResponse> downloadResult =
            await _client!.FileTransfer.GetUserFileCiphertextAsync(hashId, proof, false);

        Assert.That(downloadResult.IsRight, Is.True);
        downloadResult.DoRight(x => { Assert.DoesNotThrow(() => x.Stream.ReadByte()); });
    }
}
