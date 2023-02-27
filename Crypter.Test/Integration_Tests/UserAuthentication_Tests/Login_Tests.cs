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
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Test.Integration_Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.UserAuthentication_Tests
{
   [TestFixture]
   internal class Login_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private ICrypterApiClient _client;

      private const string _defaultUsername = "Frodo";
      private const string _defaultPassword = "The Precious";

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
      public async Task Login_Works()
      {
         RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(_defaultUsername, _defaultPassword);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetLoginRequest(_defaultUsername, _defaultPassword);
         var result = await _client.UserAuthentication.LoginAsync(loginRequest);

         Assert.True(registrationResult.IsRight);
         Assert.True(result.IsRight);
      }

      [Test]
      public async Task Login_Fails_Invalid_Username()
      {
         LoginRequest request = TestData.GetLoginRequest(_defaultUsername, _defaultPassword);
         var result = await _client.UserAuthentication.LoginAsync(request);

         Assert.True(result.IsLeft);
      }

      [Test]
      public async Task Login_Fails_Invalid_Password()
      {
         RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(_defaultUsername, _defaultPassword);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetLoginRequest(_defaultUsername, _defaultPassword);
         VersionedPassword invalidPassword = new VersionedPassword("invalid"u8.ToArray(), 1);
         loginRequest.VersionedPasswords = new List<VersionedPassword> { invalidPassword };
         var result = await _client.UserAuthentication.LoginAsync(loginRequest);

         Assert.True(registrationResult.IsRight);
         Assert.True(result.IsLeft);
      }

      [Test]
      public async Task Login_Fails_Invalid_Password_Version()
      {
         RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(_defaultUsername, _defaultPassword);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetLoginRequest(_defaultUsername, _defaultPassword);
         VersionedPassword correctPassword = loginRequest.VersionedPasswords.First();
         VersionedPassword invalidPassword = new VersionedPassword(correctPassword.Password, (short)(correctPassword.Version - 1));
         loginRequest.VersionedPasswords = new List<VersionedPassword> { invalidPassword };
         var result = await _client.UserAuthentication.LoginAsync(loginRequest);

         Assert.True(registrationResult.IsRight);
         Assert.True(result.IsLeft);
      }
   }
}
