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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record ChangeUserPasswordCommand(Guid UserId, PasswordChangeRequest Request)
    : IEitherRequest<PasswordChangeError, Unit>;

internal class ChangeUserPasswordCommandHandler
    : IEitherRequestHandler<ChangeUserPasswordCommand, PasswordChangeError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ServerPasswordSettings _serverPasswordSettings;


    public ChangeUserPasswordCommandHandler(DataContext dataContext, IPasswordHashService passwordHashService, IOptions<ServerPasswordSettings> serverPasswordSettings)
    {
        _dataContext = dataContext;
        _passwordHashService = passwordHashService;
        _serverPasswordSettings = serverPasswordSettings.Value;
    }

    public async Task<Either<PasswordChangeError, Unit>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        return await ValidatePasswordChangeRequest(request.Request)
            .BindAsync(async validPasswordChangeRequest => await (
                from foundUser in GetUserAsync(request.UserId)
                from passwordVerificationSuccess in VerifyAndChangePasswordAsync(validPasswordChangeRequest, foundUser).AsTask()
                from updateMasterKeyResult in Either<PasswordChangeError, Unit>.From(UpdateUserMasterKey(foundUser, request.Request.EncryptedMasterKey, request.Request.Nonce)).AsTask()
                from _ in Either<PasswordChangeError, Unit>.FromRightAsync(SaveChangesAsync())
                select Unit.Default)
            );
    }

    private readonly struct ValidPasswordChangeRequest(IDictionary<short, byte[]> oldVersionedPasswords, byte[] newVersionedPassword)
    {
        public IDictionary<short, byte[]> OldVersionedPasswords { get; } = oldVersionedPasswords;
        public byte[] NewVersionedPassword { get; } = newVersionedPassword;
    }

    private Either<PasswordChangeError, ValidPasswordChangeRequest> ValidatePasswordChangeRequest(PasswordChangeRequest request)
    {
        if (request.NewVersionedPassword.Version != _serverPasswordSettings.ClientVersion)
        {
            return PasswordChangeError.InvalidNewPasswordVersion;
        }

        return GetValidClientPasswords(request.OldVersionedPasswords)
            .Map(x => new ValidPasswordChangeRequest(x, request.NewVersionedPassword.Password));
    }
    
    private async Task<Either<PasswordChangeError, UserEntity>> GetUserAsync(Guid userId)
    {
        UserEntity? foundUser = await _dataContext.Users
            .Include(x => x.MasterKey)
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        if (foundUser is null)
        {
            return PasswordChangeError.UnknownError;
        }

        return foundUser;
    }
    
    private Either<PasswordChangeError, Unit> VerifyAndChangePasswordAsync(ValidPasswordChangeRequest validChangePasswordRequest, UserEntity userEntity)
    {
        bool requestContainsUserClientPasswordVersion = validChangePasswordRequest.OldVersionedPasswords.ContainsKey(userEntity.ClientPasswordVersion);
        if (!requestContainsUserClientPasswordVersion)
        {
            return PasswordChangeError.InvalidOldPasswordVersion;
        }

        // Get the appropriate 'old' password for the user, based on the saved 'ClientPasswordVersion' for the user.
        // Then hash that password based on the saved 'ServerPasswordVersion' for the user.
        byte[] currentClientPassword = validChangePasswordRequest.OldVersionedPasswords[userEntity.ClientPasswordVersion];
        bool isMatchingPassword = AuthenticationPassword.TryFrom(currentClientPassword, out AuthenticationPassword validOldPassword)
                                  && _passwordHashService.VerifySecurePasswordHash(validOldPassword, userEntity.PasswordHash, userEntity.PasswordSalt, userEntity.ServerPasswordVersion);
        
        // If the hashes do not match, then the wrong password was provided.
        if (!isMatchingPassword)
        {
            return PasswordChangeError.InvalidPassword;
        }

        // Now change the password to the newly provided password
        if (!AuthenticationPassword.TryFrom(validChangePasswordRequest.NewVersionedPassword, out AuthenticationPassword validNewPassword))
        {
            return PasswordChangeError.InvalidPassword; 
        }
        
        SecurePasswordHashOutput hashOutput = _passwordHashService.MakeSecurePasswordHash(validNewPassword, _passwordHashService.LatestServerPasswordVersion);
        
        userEntity.PasswordHash = hashOutput.Hash;
        userEntity.PasswordSalt = hashOutput.Salt;
        userEntity.ServerPasswordVersion = _passwordHashService.LatestServerPasswordVersion;
        userEntity.ClientPasswordVersion = _serverPasswordSettings.ClientVersion;
        
        return Unit.Default;
    }
    
    private Either<PasswordChangeError, IDictionary<short, byte[]>> GetValidClientPasswords(List<VersionedPassword> clientPasswords)
    {
        bool someHasInvalidClientPasswordVersion = clientPasswords.Any(x => x.Version > _serverPasswordSettings.ClientVersion || x.Version < 0);
        if (someHasInvalidClientPasswordVersion)
        {
            return PasswordChangeError.InvalidOldPasswordVersion;
        }

        bool duplicateVersionsProvided = clientPasswords.GroupBy(x => x.Version).Any(x => x.Count() > 1);
        if (duplicateVersionsProvided)
        {
            return PasswordChangeError.InvalidOldPasswordVersion;
        }

        return clientPasswords.ToDictionary(x => x.Version, x => x.Password);
    }

    private Unit UpdateUserMasterKey(UserEntity userEntity, byte[] encryptedMasterKey, byte[] nonce)
    {
        userEntity.MasterKey!.EncryptedKey = encryptedMasterKey;
        userEntity.MasterKey!.Nonce = nonce;
        userEntity.MasterKey!.Updated = DateTime.UtcNow;
        return Unit.Default;
    }

    private async Task<Unit> SaveChangesAsync()
    {
        await _dataContext.SaveChangesAsync();
        return Unit.Default;
    }
}
