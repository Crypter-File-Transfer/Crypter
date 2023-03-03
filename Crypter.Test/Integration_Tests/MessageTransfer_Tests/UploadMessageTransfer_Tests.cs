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

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Monads;
using Crypter.Crypto.Common.StreamEncryption;
using Crypter.Test.Integration_Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.MessageTransfer_Tests
{
   [TestFixture]
   internal class UploadMessageTransfer_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private ICrypterApiClient _client;

      [OneTimeSetUp]
      public async Task OneTimeSetUp()
      {
         _setup = new Setup();
         await _setup.InitializeRespawnerAsync();

         _factory = await Setup.SetupWebApplicationFactoryAsync();
         (_client, _) = Setup.SetupCrypterApiClient(_factory.CreateClient());
      }

      [TearDown]
      public async Task TearDown()
      {
         await _setup.ResetServerDataAsync();
      }

      [OneTimeTearDown]
      public async Task OneTimeTearDown()
      {
         await _factory.DisposeAsync();
      }

      [Test]
      public async Task Upload_Anonymous_Message_Transfer_Works()
      {
         (EncryptionStream encryptionStream, byte[] keyExchangeProof) = TestData.GetDefaultEncryptionStream();
         UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof, TestData.DefaultTransferLifetimeHours);
         var result = await _client.MessageTransfer.UploadMessageTransferAsync(Maybe<string>.None, request, encryptionStream, false);

         Assert.True(result.IsRight);
      }

      [Test]
      public async Task Upload_User_Message_Transfer_Fails_When_Recipient_Does_Not_Exist()
      {
         (EncryptionStream encryptionStream, byte[] keyExchangeProof) = TestData.GetDefaultEncryptionStream();
         UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof, TestData.DefaultTransferLifetimeHours);
         var result = await _client.MessageTransfer.UploadMessageTransferAsync("John Smith", request, encryptionStream, false);

         Assert.True(result.IsLeft);
      }

      [Test]
      public void Upload_Authenticated_Message_Transfer_Throws_When_Not_Authenticated()
      {
         (EncryptionStream encryptionStream, byte[] keyExchangeProof) = TestData.GetDefaultEncryptionStream();
         UploadMessageTransferRequest request = new UploadMessageTransferRequest(TestData.DefaultTransferMessageSubject, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce, keyExchangeProof, TestData.DefaultTransferLifetimeHours);

         Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.MessageTransfer.UploadMessageTransferAsync(Maybe<string>.None, request, encryptionStream, true));
      }
   }
}
