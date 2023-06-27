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
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserSettings.PrivacySettings;
using Crypter.Common.Enums;
using Crypter.Test.Integration_Tests.Common;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.UserSettings_Tests
{
   [TestFixture]
   internal class SetPrivacySettings_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private ICrypterApiClient _client;
      private ITokenRepository _clientTokenRepository;

      [OneTimeSetUp]
      public async Task OneTimeSetUp()
      {
         _setup = new Setup();
         await _setup.InitializeRespawnerAsync();

         _factory = await Setup.SetupWebApplicationFactoryAsync();
         (_client, _clientTokenRepository) = Setup.SetupCrypterApiClient(_factory.CreateClient());
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
      public async Task Set_Privacy_Settings_Work_async()
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

         PrivacySettings request = new PrivacySettings(false, UserVisibilityLevel.None, UserItemTransferPermission.None, UserItemTransferPermission.None);
         Either<SetPrivacySettingsError, PrivacySettings> result = await _client.UserSetting.SetPrivacySettingsAsync(request);

         Assert.True(result.IsRight);
      }
   }
}
