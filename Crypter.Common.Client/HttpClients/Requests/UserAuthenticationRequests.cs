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

using System;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.UserAuthentication;
using EasyMonads;

namespace Crypter.Common.Client.HttpClients.Requests;

public class UserAuthenticationRequests : IUserAuthenticationRequests
{
    private readonly ICrypterHttpClient _crypterHttpClient;
    private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;
    private readonly EventHandler _refreshTokenRejectedHandler;

    public UserAuthenticationRequests(ICrypterHttpClient crypterHttpClient,
        ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient, EventHandler refreshTokenRejectedHandler)
    {
        _crypterHttpClient = crypterHttpClient;
        _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
        _refreshTokenRejectedHandler = refreshTokenRejectedHandler;
    }

    public Task<Either<RegistrationError, Unit>> RegisterAsync(RegistrationRequest registerRequest)
    {
        string url = "api/user/authentication/register";
        return _crypterHttpClient.PostEitherUnitResponseAsync(url, registerRequest)
            .ExtractErrorCode<RegistrationError, Unit>();
    }

    public Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        string url = "api/user/authentication/login";
        return _crypterHttpClient.PostEitherAsync<LoginRequest, LoginResponse>(url, loginRequest)
            .ExtractErrorCode<LoginError, LoginResponse>();
    }

    public async Task<Either<RefreshError, RefreshResponse>> RefreshSessionAsync()
    {
        string url = "api/user/authentication/refresh";
        Either<RefreshError, RefreshResponse> response = await _crypterAuthenticatedHttpClient
            .GetEitherAsync<RefreshResponse>(url, true)
            .ExtractErrorCode<RefreshError, RefreshResponse>();

        response.DoLeftOrNeither(() => _refreshTokenRejectedHandler?.Invoke(this, EventArgs.Empty));
        return response;
    }

    public Task<Either<PasswordChallengeError, Unit>> PasswordChallengeAsync(
        PasswordChallengeRequest testPasswordRequest)
    {
        string url = "api/user/authentication/password/challenge";
        return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, testPasswordRequest)
            .ExtractErrorCode<PasswordChallengeError, Unit>();
    }

    public Task<Either<LogoutError, Unit>> LogoutAsync()
    {
        string url = "api/user/authentication/logout";
        return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, true)
            .ExtractErrorCode<LogoutError, Unit>();
    }
}
