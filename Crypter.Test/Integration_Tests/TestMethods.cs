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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Crypto.Providers.Default;
using EasyMonads;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests;

internal static class TestMethods
{
    internal static async Task LoginAsync(ICrypterApiClient apiClient, ITokenRepository tokenRepository)
    {
        RegistrationRequest registrationRequest = TestData.GetRegistrationRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<RegistrationError, Unit> registrationResult = await apiClient.UserAuthentication.RegisterAsync(registrationRequest);
    
        LoginRequest loginRequest = TestData.GetLoginRequest(TestData.DefaultUsername, TestData.DefaultPassword);
        Either<LoginError, LoginResponse> loginResult = await apiClient.UserAuthentication.LoginAsync(loginRequest);

        await loginResult.DoRightAsync(async loginResponse =>
        {
            await tokenRepository.StoreAuthenticationTokenAsync(loginResponse.AuthenticationToken);
            await tokenRepository.StoreRefreshTokenAsync(loginResponse.RefreshToken, TokenType.Session);
        });
    }
    
    internal static async Task<string> InitiateMultipartFileTransferAsync(ICrypterApiClient apiClient, ITokenRepository tokenRepository)
    {
        await LoginAsync(apiClient, tokenRepository);
        
        DefaultCryptoProvider cryptoProvider = new DefaultCryptoProvider();
        (_, byte[] proof) = cryptoProvider.KeyExchange.GenerateEncryptionKey(
            cryptoProvider.StreamEncryptionFactory.KeySize, TestData.DefaultPrivateKey, TestData.AlternatePublicKey,
            TestData.DefaultKeyExchangeNonce);
    
        UploadFileTransferRequest request = new UploadFileTransferRequest(TestData.DefaultTransferFileName,
            TestData.DefaultTransferFileContentType, TestData.DefaultPublicKey, TestData.DefaultKeyExchangeNonce,
            proof, TestData.DefaultTransferLifetimeHours);
        Either<UploadTransferError, InitiateMultipartFileTransferResponse> initializationResult =
            await apiClient.FileTransfer.InitializeMultipartFileTransferAsync(Maybe<string>.None, request);

        return initializationResult
            .Select(x => x.HashId)
            .DoLeftOrNeither(Assert.Fail)
            .RightOrDefault(string.Empty);
    }
}
