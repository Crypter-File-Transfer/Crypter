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

using Crypter.Common.Client.Implementations;
using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Test.Integration_Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.UserAuthentication_Tests
{
   [TestFixture]
   internal class PasswordChallenge_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private HttpClient _baseClient;
      private ICrypterApiClient _client;
      private ITokenRepository _clientTokenRepository;

      [OneTimeSetUp]
      public async Task OneTimeSetUp()
      {
         _setup = new Setup();
         await _setup.InitializeRespawnerAsync();

         _factory = await Setup.SetupWebApplicationFactoryAsync();
         _baseClient = _factory.CreateClient();
         (_client, _clientTokenRepository) = Setup.SetupCrypterApiClient(_baseClient);
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
      public async Task Password_Challenge_Works()
      {
         RegistrationRequest registrationRequest = TestData.GetDefaultRegistrationRequest(false);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetDefaultLoginRequest(TokenType.Session);
         var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

         await loginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         PasswordChallengeRequest request = new PasswordChallengeRequest(registrationRequest.VersionedPassword.Password);
         var result = await _client.UserAuthentication.PasswordChallengeAsync(request);

         Assert.True(registrationResult.IsRight);
         Assert.True(loginResult.IsRight);
         Assert.True(result.IsRight);
      }

      [Test]
      public async Task Password_Challenge_Fails_Not_Authenticated()
      {
         RegistrationRequest registrationRequest = TestData.GetDefaultRegistrationRequest(false);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         PasswordChallengeRequest request = new PasswordChallengeRequest(registrationRequest.VersionedPassword.Password);
         using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/user/authentication/password/challenge")
         {
            Content = JsonContent.Create(request)
         };
         HttpResponseMessage response = await _baseClient.SendAsync(requestMessage);

         Assert.True(registrationResult.IsRight);
         Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
      }

      [Test]
      public async Task Password_Challenge_Fails_Wrong_Password()
      {
         RegistrationRequest registrationRequest = TestData.GetDefaultRegistrationRequest(false);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetDefaultLoginRequest(TokenType.Session);
         var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

         await loginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         byte[] wrongPassword = new byte[registrationRequest.VersionedPassword.Password.Length];
         registrationRequest.VersionedPassword.Password.CopyTo(wrongPassword, 0);
         wrongPassword[0] = (byte)(wrongPassword[0] == 0x01
            ? 0x02
            : 0x01);

         PasswordChallengeRequest request = new PasswordChallengeRequest(wrongPassword);
         var result = await _client.UserAuthentication.PasswordChallengeAsync(request);

         Assert.True(registrationResult.IsRight);
         Assert.True(loginResult.IsRight);
         Assert.AreNotEqual(registrationRequest.VersionedPassword.Password, wrongPassword);
         Assert.True(result.IsLeft);
      }
   }
}
