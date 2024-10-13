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
using Crypter.Core.Features.UserAuthentication.Events;
using Crypter.Core.Identity;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;


namespace Crypter.Core.Features.UserAuthentication;

internal static class Common
{
    internal static async Task<Unit> PublishRefreshTokenCreatedEventAsync(IPublisher publisher, RefreshTokenData refreshTokenData)
    {
        RefreshTokenCreatedEvent refreshTokenCreatedEvent = new RefreshTokenCreatedEvent(
            refreshTokenData.TokenId,
            refreshTokenData.Expiration);
        await publisher.Publish(refreshTokenCreatedEvent);
        return Unit.Default;
    }
    
    internal static async Task<Either<PasswordChallengeError, Unit>> TestUserPasswordAsync(
        DataContext dataContext,
        IPasswordHashService passwordHashService,
        Guid userId,
        byte[] authenticationPassword,
        short clientPasswordVersion,
        CancellationToken cancellationToken = default)
    {
        return await (from validAuthenticationPassword in ValidateRequestPassword(authenticationPassword,
                PasswordChallengeError.InvalidPassword).AsTask()
            from user in FetchUserAsync(PasswordChallengeError.UnknownError)
            from unit0 in VerifyUserPasswordIsMigrated(user, PasswordChallengeError.PasswordNeedsMigration)
                .ToLeftEither(Unit.Default).AsTask()
            from passwordVerified in VerifyPassword(user.PasswordHash, user.PasswordSalt,
                passwordHashService.LatestServerPasswordVersion)
                ? Either<PasswordChallengeError, Unit>.FromRight(Unit.Default).AsTask()
                : Either<PasswordChallengeError, Unit>.FromLeft(PasswordChallengeError.InvalidPassword).AsTask()
            select Unit.Default);
        
        static Either<T, AuthenticationPassword> ValidateRequestPassword<T>(byte[] password, T error)
        {
            return AuthenticationPassword.TryFrom(password, out AuthenticationPassword validAuthenticationPassword)
                ? validAuthenticationPassword
                : error;
        }

        Maybe<T> VerifyUserPasswordIsMigrated<T>(UserEntity user, T error)
        {
            return user.ClientPasswordVersion == clientPasswordVersion &&
                   user.ServerPasswordVersion == passwordHashService.LatestServerPasswordVersion
                ? Maybe<T>.None
                : error;
        }

        Task<Either<T, UserEntity?>> FetchUserAsync<T>(T error)
        {
            return Either<T, UserEntity?>.FromRightAsync(
                dataContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken),
                error);
        }

        bool VerifyPassword(byte[] existingPasswordHash, byte[] passwordSalt,
            short serverPasswordVersion)
        {
            return AuthenticationPassword.TryFrom(authenticationPassword, out AuthenticationPassword validAuthenticationPassword)
                && passwordHashService.VerifySecurePasswordHash(validAuthenticationPassword, existingPasswordHash, passwordSalt,
                    serverPasswordVersion);
        }
    }
}
