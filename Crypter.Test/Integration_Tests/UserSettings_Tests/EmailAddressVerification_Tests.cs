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
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Core;
using Crypter.Core.Entities;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.CryptoHash;
using Crypter.Crypto.Common.DigitalSignature;
using Crypter.Crypto.Providers.Default;
using Crypter.Crypto.Providers.Default.Wrappers;
using Crypter.Test.Integration_Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.UserSettings_Tests
{
   [TestFixture]
   internal class EmailAddressVerification_Tests
   {
      private Setup _setup;
      private WebApplicationFactory<Program> _factory;
      private ICrypterApiClient _client;
      private ITokenRepository _clientTokenRepository;

      DefaultCryptoProvider _cryptoProvider;
      private Ed25519KeyPair _emailVerificationKeyPair;

      [OneTimeSetUp]
      public async Task OneTimeSetUp()
      {
         _setup = new Setup();
         await _setup.InitializeRespawnerAsync();

         _cryptoProvider = new DefaultCryptoProvider();
         _emailVerificationKeyPair = _cryptoProvider.DigitalSignature.GenerateKeyPair();

         ICryptoProvider mockCryptoProvider = CreateMockCryptoProvider(_emailVerificationKeyPair).Object;
         var overrideServices = new ServiceCollection();
         overrideServices.AddSingleton(mockCryptoProvider);

         _factory = await Setup.SetupWebApplicationFactoryAsync(overrideServices);
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

      private static Mock<ICryptoProvider> CreateMockCryptoProvider(Ed25519KeyPair keyPairToReturn)
      {
         Mock<ICryptoProvider> cryptoProviderMock = new Mock<ICryptoProvider>(MockBehavior.Strict)
         {
            CallBase = true
         };

         cryptoProviderMock.Setup(x => x.ConstantTime)
            .Returns(new ConstantTime());

         cryptoProviderMock.Setup(x => x.CryptoHash)
            .Returns(new CryptoHash());

         cryptoProviderMock.Setup(x => x.DigitalSignature)
            .Returns(new DigitalSignature());

         cryptoProviderMock.Setup(x => x.GenericHash)
            .Returns(new GenericHash());

         cryptoProviderMock.Setup(x => x.Padding)
            .Returns(new Padding());

         cryptoProviderMock.Setup(x => x.Random)
            .Returns(new Random());

         Mock<DigitalSignature> digitalSignatureMock = new Mock<DigitalSignature>
         {
            CallBase = true
         };
         digitalSignatureMock.Setup(x => x.GenerateKeyPair())
            .Returns(keyPairToReturn);

         cryptoProviderMock.Setup(x => x.DigitalSignature)
            .Returns(digitalSignatureMock.Object);

         return cryptoProviderMock;
      }

      [Test]
      public async Task Email_Address_Verification_Works_Async()
      {
         RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword, TestData.DefaultEmailAdress);
         var registrationResult = await _client.UserAuthentication.RegisterAsync(registrationRequest);

         LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword, TokenType.Session);
         var loginResult = await _client.UserAuthentication.LoginAsync(loginRequest);

         await loginResult.DoRightAsync(async loginResponse =>
         {
            await _clientTokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await _clientTokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
         });

         // Allow the background service to "send" the email and save the email verification data
         await Task.Delay(5000);

         DataContext dataContext = _factory.Services.GetRequiredService<DataContext>();
         UserEmailVerificationEntity verificationData = await dataContext.UserEmailVerifications
            .Where(x => x.User.Username == TestData.DefaultUsername)
            .FirstOrDefaultAsync();

         string encodedVerificationCode = EmailVerificationEncoder.EncodeVerificationCodeUrlSafe(verificationData.Code);
         byte[] signedVerificationCode = _cryptoProvider.DigitalSignature.GenerateSignature(_emailVerificationKeyPair.PrivateKey, verificationData.Code.ToByteArray());
         string encodedSignature = EmailVerificationEncoder.EncodeSignatureUrlSafe(signedVerificationCode);

         VerifyEmailAddressRequest request = new VerifyEmailAddressRequest(encodedVerificationCode, encodedSignature);
         Either<VerifyEmailAddressError, Unit> result = await _client.UserSetting.VerifyUserEmailAddressAsync(request);

         Assert.True(result.IsRight);
      }
   }
}
