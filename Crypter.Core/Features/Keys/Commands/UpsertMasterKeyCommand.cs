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

using System;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.Keys.Commands;

public sealed record UpsertMasterKeyCommand(Guid UserId, InsertMasterKeyRequest Data, bool AllowReplacement)
    : IEitherRequest<InsertMasterKeyError, Unit>;

internal class UpsertMasterKeyCommandHandler : IEitherRequestHandler<UpsertMasterKeyCommand, InsertMasterKeyError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    
    private readonly short _clientPasswordVersion;

    public UpsertMasterKeyCommandHandler(
        DataContext dataContext,
        IPasswordHashService passwordHashService,
        IOptions<ServerPasswordSettings> passwordSettings)
    {
        _dataContext = dataContext;
        _passwordHashService = passwordHashService;
        
        _clientPasswordVersion = passwordSettings.Value.ClientVersion;
    }

    public async Task<Either<InsertMasterKeyError, Unit>> Handle(UpsertMasterKeyCommand request, CancellationToken cancellationToken)
    {
        if (!MasterKeyValidators.ValidateMasterKeyInformation(request.Data.EncryptedKey, request.Data.Nonce, request.Data.RecoveryProof))
        {
            return InsertMasterKeyError.InvalidMasterKey;
        }
        
        Either<PasswordChallengeError, Unit> testPasswordResult = await UserAuthentication.Common.TestUserPasswordAsync(
            _dataContext,
            _passwordHashService,
            request.UserId,
            request.Data.AuthenticationPassword,
            _clientPasswordVersion,
            CancellationToken.None);
        
        return await testPasswordResult.MatchAsync<Either<InsertMasterKeyError, Unit>>(
            _ => InsertMasterKeyError.InvalidPassword,
            async _ =>
            {
                UserMasterKeyEntity? masterKeyEntity = await _dataContext.UserMasterKeys
                    .FirstOrDefaultAsync(x => x.Owner == request.UserId, CancellationToken.None);

                if (masterKeyEntity is not null && !request.AllowReplacement)
                {
                    return InsertMasterKeyError.Conflict;
                }

                DateTime now = DateTime.UtcNow;
                UserMasterKeyEntity newEntity = new UserMasterKeyEntity(request.UserId, request.Data.EncryptedKey,
                    request.Data.Nonce, request.Data.RecoveryProof, now, now);
                _dataContext.UserMasterKeys.Add(newEntity);

                await _dataContext.SaveChangesAsync(CancellationToken.None);
                return Unit.Default;
            },
            InsertMasterKeyError.UnknownError);
    }
}
