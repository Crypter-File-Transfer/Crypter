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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Core.Features.Keys;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Services;

public interface IUserKeysService
{
    Task<Either<InsertMasterKeyError, Unit>> UpsertMasterKeyAsync(Guid userId, InsertMasterKeyRequest request,
        bool allowReplacement);

    Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(Guid userId,
        InsertKeyPairRequest request);

    Task DeleteUserKeysAsync(Guid userId);
}

public class UserKeysService : IUserKeysService
{
    private readonly DataContext _context;
    private readonly IUserAuthenticationService _userAuthenticationService;

    public UserKeysService(DataContext context, IUserAuthenticationService userAuthenticationService)
    {
        _context = context;
        _userAuthenticationService = userAuthenticationService;
    }
    
    public async Task<Either<InsertMasterKeyError, Unit>> UpsertMasterKeyAsync(Guid userId,
        InsertMasterKeyRequest request, bool allowReplacement)
    {
        if (!MasterKeyValidators.ValidateMasterKeyInformation(request.EncryptedKey, request.Nonce,
                request.RecoveryProof))
        {
            return InsertMasterKeyError.InvalidMasterKey;
        }

        var testPasswordResult =
            await _userAuthenticationService.TestUserPasswordAsync(userId,
                new PasswordChallengeRequest(request.Password));
        return await testPasswordResult.MatchAsync<Either<InsertMasterKeyError, Unit>>(
            error => InsertMasterKeyError.InvalidPassword,
            async _ =>
            {
                var masterKeyEntity = await _context.UserMasterKeys
                    .FirstOrDefaultAsync(x => x.Owner == userId);

                if (masterKeyEntity is not null && !allowReplacement)
                {
                    return InsertMasterKeyError.Conflict;
                }

                DateTime now = DateTime.UtcNow;
                var newEntity = new UserMasterKeyEntity(userId, request.EncryptedKey, request.Nonce,
                    request.RecoveryProof, now, now);
                _context.UserMasterKeys.Add(newEntity);

                await _context.SaveChangesAsync();
                return Unit.Default;
            },
            InsertMasterKeyError.UnknownError);
    }

    public async Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(Guid userId,
        InsertKeyPairRequest request)
    {
        var keyPairEntity = await _context.UserKeyPairs
            .FirstOrDefaultAsync(x => x.Owner == userId);

        if (keyPairEntity is null)
        {
            var newEntity = new UserKeyPairEntity(userId, request.EncryptedPrivateKey, request.PublicKey, request.Nonce,
                DateTime.UtcNow);
            _context.UserKeyPairs.Add(newEntity);
            await _context.SaveChangesAsync();
        }

        return keyPairEntity is null
            ? new InsertKeyPairResponse()
            : InsertKeyPairError.KeyPairAlreadyExists;
    }

    public async Task DeleteUserKeysAsync(Guid userId)
    {
        UserMasterKeyEntity masterKey = await _context.UserMasterKeys
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync();

        if (masterKey is not null)
        {
            _context.Remove(masterKey);
        }

        List<UserKeyPairEntity> keyPairs = await _context.UserKeyPairs
            .Where(x => x.Owner == userId)
            .ToListAsync();

        _context.RemoveRange(keyPairs);

        await _context.SaveChangesAsync();
    }
}
