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
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.Keys;
using EasyMonads;

namespace Crypter.Common.Client.HttpClients.Requests;

public class UserKeyRequests : IUserKeyRequests
{
    private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

    public UserKeyRequests(ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient)
    {
        _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
    }

    public Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync()
    {
        string url = "api/user/key/master";
        return _crypterAuthenticatedHttpClient.GetEitherAsync<GetMasterKeyResponse>(url)
            .ExtractErrorCode<GetMasterKeyError, GetMasterKeyResponse>();
    }

    public Task<Either<InsertMasterKeyError, Unit>> InsertMasterKeyAsync(InsertMasterKeyRequest request)
    {
        string url = "api/user/key/master";
        return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, request)
            .ExtractErrorCode<InsertMasterKeyError, Unit>();
    }

    public Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>>
        GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request)
    {
        string url = "api/user/key/master/recovery-proof/challenge";
        return _crypterAuthenticatedHttpClient
            .PostEitherAsync<GetMasterKeyRecoveryProofRequest, GetMasterKeyRecoveryProofResponse>(url, request)
            .ExtractErrorCode<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>();
    }

    public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync()
    {
        string url = "api/user/key/private";
        return _crypterAuthenticatedHttpClient.GetEitherAsync<GetPrivateKeyResponse>(url)
            .ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>();
    }

    public Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(InsertKeyPairRequest request)
    {
        string url = "api/user/key/private";
        return _crypterAuthenticatedHttpClient.PutEitherAsync<InsertKeyPairRequest, InsertKeyPairResponse>(url, request)
            .ExtractErrorCode<InsertKeyPairError, InsertKeyPairResponse>();
    }
}
