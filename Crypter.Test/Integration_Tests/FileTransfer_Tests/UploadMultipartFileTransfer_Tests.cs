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

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.FileTransfer_Tests;

[TestFixture]
public sealed class UploadMultipartFileTransfer_Tests
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
    public async Task Upload_Multipart_File_Transfer_Works()
    {
        string hashId = await TestMethods.InitiateMultipartFileTransferAsync(_client!, _clientTokenRepository!);
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, _) = TestData.GetDefaultEncryptionStream();
        Either<UploadMultipartFileTransferError, Unit> uploadResult =
            await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, 0, () => encryptionStreamOpener(null));
        
        Assert.That(uploadResult.IsRight, Is.True);
    }
    
    [Test]
    public async Task Upload_Multipart_File_Transfer_Throws_When_Not_Authenticated()
    {
        string hashId = await TestMethods.InitiateMultipartFileTransferAsync(_client!, _clientTokenRepository!);
        
        await _clientTokenRepository!.DeleteAuthenticationTokenAsync();
        await _clientTokenRepository!.DeleteRefreshTokenAsync();
        
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, _) = TestData.GetDefaultEncryptionStream();
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, 0, () => encryptionStreamOpener(null)));
    }
    
    [Test]
    public async Task Upload_Multipart_File_Transfer_Requires_Authorization()
    {
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, _) = TestData.GetDefaultEncryptionStream();
        using HttpRequestMessage requestMessage =
            new HttpRequestMessage(HttpMethod.Post, "api/file/transfer/multipart/upload")
            {
                Content = new MultipartFormDataContent
                {
                    { new StreamContent(encryptionStreamOpener(null)), "Ciphertext", "Ciphertext" }
                }
            };
        using HttpClient rawClient = _factory!.CreateClient();
        HttpResponseMessage response = await rawClient.SendAsync(requestMessage);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Upload_Multipart_File_Transfer_Fails_When_Not_Initialized()
    {
        await TestMethods.LoginAsync(_client!, _clientTokenRepository!);
        const string uninitializedHashId = "test";
        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, _) = TestData.GetDefaultEncryptionStream();
        Either<UploadMultipartFileTransferError, Unit> response = await _client!.FileTransfer.UploadMultipartFileTransferAsync(uninitializedHashId, 0,
            () => encryptionStreamOpener(null));
        
        Assert.That(response.IsLeft, Is.True);
        response.DoLeftOrNeither(
            left: x => Assert.That(x, Is.EqualTo(UploadMultipartFileTransferError.NotFound)),
            neither: Assert.Fail);
    }

    [Test]
    public async Task Upload_Multipart_File_Transfer_Fails_When_Aggregate_Becomes_Too_Large()
    {
        Random random = new Random();
        int bufferSize = Convert.ToInt32(Math.Pow(10, 5)) + 1;
        byte[] buffer = new byte[bufferSize];
        random.NextBytes(buffer);
        
        string hashId = await TestMethods.InitiateMultipartFileTransferAsync(_client!, _clientTokenRepository!);
        
        for (int i = 0; i < 10; i++)
        {
            Either<UploadMultipartFileTransferError, Unit> response = await _client!.FileTransfer.UploadMultipartFileTransferAsync(hashId, i, () => new MemoryStream(buffer));

            if (i < 9)
            {
                Assert.That(response.IsRight, Is.True);
            }
            else
            {
                // The final request should exceed the maximum, aggregate file size
                Assert.That(response.IsLeft, Is.True);
                response.DoLeftOrNeither(
                    left: x => Assert.That(x, Is.EqualTo(UploadMultipartFileTransferError.AggregateTooLarge)),
                    neither: Assert.Fail);
            }
        }
    }
}
