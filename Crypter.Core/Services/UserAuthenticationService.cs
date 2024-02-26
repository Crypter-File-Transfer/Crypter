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
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Core.Identity;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Services;

public interface IUserAuthenticationService
{
    Task<Either<PasswordChallengeError, Unit>> TestUserPasswordAsync(Guid userId, PasswordChallengeRequest request,
        CancellationToken cancellationToken = default);
}

public static class UserAuthenticationServiceExtensions
{
    public static void AddUserAuthenticationService(this IServiceCollection services,
        Action<ServerPasswordSettings> settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        services.Configure(settings);
        services.TryAddScoped<IUserAuthenticationService, UserAuthenticationService>();
    }
}

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly DataContext _context;
    private readonly IPasswordHashService _passwordHashService;

    private readonly short _clientPasswordVersion;

    public UserAuthenticationService(
        DataContext context,
        IPasswordHashService passwordHashService,
        IOptions<ServerPasswordSettings> passwordSettings)
    {
        _context = context;
        _passwordHashService = passwordHashService;

        _clientPasswordVersion = passwordSettings.Value.ClientVersion;
    }

    public Task<Either<PasswordChallengeError, Unit>> TestUserPasswordAsync(Guid userId,
        PasswordChallengeRequest request, CancellationToken cancellationToken = default)
    {
        return from validAuthenticationPassword in ValidateRequestPassword(request.AuthenticationPassword,
                PasswordChallengeError.InvalidPassword).AsTask()
            from user in FetchUserAsync(userId, PasswordChallengeError.UnknownError, cancellationToken)
            from unit0 in VerifyUserPasswordIsMigrated(user, PasswordChallengeError.PasswordNeedsMigration)
                .ToLeftEither(Unit.Default).AsTask()
            from passwordVerified in VerifyPassword(validAuthenticationPassword, user.PasswordHash, user.PasswordSalt,
                _passwordHashService.LatestServerPasswordVersion)
                ? Either<PasswordChallengeError, Unit>.FromRight(Unit.Default).AsTask()
                : Either<PasswordChallengeError, Unit>.FromLeft(PasswordChallengeError.InvalidPassword).AsTask()
            select Unit.Default;
    }

    private static Either<T, AuthenticationPassword> ValidateRequestPassword<T>(byte[] password, T error)
    {
        return AuthenticationPassword.TryFrom(password, out AuthenticationPassword validAuthenticationPassword)
            ? validAuthenticationPassword
            : error;
    }

    private Maybe<T> VerifyUserPasswordIsMigrated<T>(UserEntity user, T error)
    {
        return user.ClientPasswordVersion == _clientPasswordVersion &&
               user.ServerPasswordVersion == _passwordHashService.LatestServerPasswordVersion
            ? Maybe<T>.None
            : error;
    }

    private Task<Either<T, UserEntity?>> FetchUserAsync<T>(Guid userId, T error,
        CancellationToken cancellationToken = default)
    {
        return Either<T, UserEntity?>.FromRightAsync(
            _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken),
            error);
    }

    private bool VerifyPassword(AuthenticationPassword authenticationPassword, byte[] existingPasswordHash, byte[] passwordSalt,
        short serverPasswordVersion)
    {
        return _passwordHashService.VerifySecurePasswordHash(authenticationPassword, existingPasswordHash, passwordSalt,
            serverPasswordVersion);
    }
}
