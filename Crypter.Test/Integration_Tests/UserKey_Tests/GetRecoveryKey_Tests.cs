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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserKey_Tests;

[TestFixture]
internal class GetRecoveryKey_Tests
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
   public async Task Get_Master_Key_Recovery_Key_Works()
   {
      RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
      var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

      LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword, TokenType.Session);
      var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

      await loginResult.DoRightAsync(async loginResponse =>
      {
         await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
         await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
      });

      (_, InsertMasterKeyRequest insertMasterKeyRequest) = TestData.GetInsertMasterKeyRequest(TestData.DefaultPassword);
      Either<InsertMasterKeyError, Unit> insertMasterKeyResult = await _client.UserKey.InsertMasterKeyAsync(insertMasterKeyRequest);

      GetMasterKeyRecoveryProofRequest request = new GetMasterKeyRecoveryProofRequest(TestData.DefaultUsername, registrationRequest.VersionedPassword.Password);
      Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse> result = await _client.UserKey.GetMasterKeyRecoveryProofAsync(request);

      GetMasterKeyRecoveryProofResponse response = result.RightOrDefault(null);

      Assert.True(insertMasterKeyResult.IsRight);
      Assert.True(result.IsRight);
      Assert.NotNull(response);

      Assert.AreEqual(insertMasterKeyRequest.RecoveryProof, response.Proof);
   }

   [Test]
   public async Task Get_Master_Key_Recovery_Key_Fails_Invalid_Password()
   {
      RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
      var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

      LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword, TokenType.Session);
      var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

      await loginResult.DoRightAsync(async loginResponse =>
      {
         await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
         await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
      });

      (_, InsertMasterKeyRequest insertMasterKeyRequest) = TestData.GetInsertMasterKeyRequest(TestData.DefaultPassword);
      Either<InsertMasterKeyError, Unit> insertMasterKeyResult = await _client.UserKey.InsertMasterKeyAsync(insertMasterKeyRequest);

      byte[] invalidPassword = "invalid"u8.ToArray();
      GetMasterKeyRecoveryProofRequest request = new GetMasterKeyRecoveryProofRequest(TestData.DefaultUsername, invalidPassword);
      Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse> result = await _client.UserKey.GetMasterKeyRecoveryProofAsync(request);

      Assert.True(insertMasterKeyResult.IsRight);
      Assert.True(result.IsLeft);
   }
}