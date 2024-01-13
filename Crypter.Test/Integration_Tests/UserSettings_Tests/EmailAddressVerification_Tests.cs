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

using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Common.Infrastructure;
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

namespace Crypter.Test.Integration_Tests.UserSettings_Tests;

[TestFixture]
internal class EmailAddressVerification_Tests
{
    private WebApplicationFactory<Program> _factory;
    private ICrypterApiClient _client;
    
    private DefaultCryptoProvider _cryptoProvider;
    private Ed25519KeyPair _emailVerificationKeyPair;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        _cryptoProvider = new DefaultCryptoProvider();
        _emailVerificationKeyPair = _cryptoProvider.DigitalSignature.GenerateKeyPair();
    }

    [SetUp]
    public async Task SetupTestAsync()
    {
        ICryptoProvider mockCryptoProvider = Mocks.CreateDeterministicCryptoProvider(_emailVerificationKeyPair).Object;
        IServiceCollection overrideServices = new ServiceCollection();
        overrideServices.AddSingleton(mockCryptoProvider);

        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync(true, overrideServices);
        (_client, _) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
    }

    [Test]
    public async Task Email_Address_Verification_Works_Async()
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername,
            TestData.DefaultPassword, TestData.DefaultEmailAdress);
        Either<RegistrationError, Unit> _ =
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
            _cryptoProvider.DigitalSignature.GenerateSignature(_emailVerificationKeyPair.PrivateKey,
                verificationData.Code.ToByteArray());
        string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(signedVerificationCode);

        VerifyEmailAddressRequest request = new VerifyEmailAddressRequest(encodedVerificationCode, encodedSignature);
        Either<VerifyEmailAddressError, Unit> result = await _client.UserSetting.VerifyUserEmailAddressAsync(request);

        Assert.That(result.IsRight, Is.True);
    }
}
