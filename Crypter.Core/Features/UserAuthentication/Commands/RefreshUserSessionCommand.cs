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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record RefreshUserSessionCommand(ClaimsPrincipal ClaimsPrincipal, string DeviceDescription)
    : IEitherRequest<RefreshError, RefreshResponse>;

internal class RefreshUserSessionCommandHandler
    : IEitherRequestHandler<RefreshUserSessionCommand, RefreshError, RefreshResponse>
{
    private readonly DataContext _dataContext;
    private readonly IPublisher _publisher;
    private readonly ITokenService _tokenService;
    
    private readonly Dictionary<TokenType, Func<Guid, RefreshTokenData>> _refreshTokenProviderMap;

    public RefreshUserSessionCommandHandler(
        DataContext dataContext,
        IPublisher publisher,
        ITokenService tokenService)
    {
        _dataContext = dataContext;
        _publisher = publisher;
        _tokenService = tokenService;
        
        _refreshTokenProviderMap = new Dictionary<TokenType, Func<Guid, RefreshTokenData>>()
        {
            { TokenType.Session, _tokenService.NewSessionToken },
            { TokenType.Device, _tokenService.NewDeviceToken }
        };
    }
    
    public async Task<Either<RefreshError, RefreshResponse>> Handle(
        RefreshUserSessionCommand request,
        CancellationToken cancellationToken)
    {
        return await (from userId in TokenService.TryParseUserId(request.ClaimsPrincipal)
                .ToEither(RefreshError.InvalidToken)
                .AsTask()
            from tokenId in TokenService.TryParseTokenId(request.ClaimsPrincipal)
                .ToEither(RefreshError.InvalidToken)
                .AsTask()
            from databaseToken in FetchUserTokenAsync(tokenId).ToEitherAsync(RefreshError.InvalidToken)
            from databaseTokenValidated in ValidateUserToken(databaseToken, userId)
                .ToEither(RefreshError.InvalidToken)
                .AsTask()
            let databaseTokenDeleted = _dataContext.UserTokens.Remove(databaseToken)
            from foundUser in FetchUserAsync(userId)
            let _ = foundUser.LastLogin = DateTime.UtcNow
            let newRefreshTokenData = CreateRefreshTokenInContext(userId, databaseToken.Type, request.DeviceDescription)
            let authenticationToken = _tokenService.NewAuthenticationToken(userId)
            from entriesModified in Either<RefreshError, int>
                .FromRightAsync(_dataContext.SaveChangesAsync(CancellationToken.None))
            from __ in Either<RefreshError, Unit>
                .FromRightAsync(Common.PublishRefreshTokenCreatedEventAsync(_publisher, newRefreshTokenData))
            select new RefreshResponse(authenticationToken, newRefreshTokenData.Token, databaseToken.Type));
    }

    private Task<Maybe<UserTokenEntity>> FetchUserTokenAsync(Guid tokenId)
    {
        return Maybe<UserTokenEntity>.FromNullableAsync(_dataContext.UserTokens
            .FindAsync([tokenId]).AsTask());
    }
    
    private static Maybe<Unit> ValidateUserToken(UserTokenEntity token, Guid userId)
    {
        bool isTokenValid = token.Owner == userId
                            && token.Expiration >= DateTime.UtcNow;

        return isTokenValid
            ? Unit.Default
            : Maybe<Unit>.None;
    }
    
    private RefreshTokenData CreateRefreshTokenInContext(Guid userId, TokenType tokenType, string deviceDescription)
    {
        RefreshTokenData refreshToken = _refreshTokenProviderMap[tokenType].Invoke(userId);
        UserTokenEntity tokenEntity = new UserTokenEntity(
            refreshToken.TokenId, 
            userId,
            deviceDescription,
            tokenType,
            refreshToken.Created,
            refreshToken.Expiration);
        _dataContext.UserTokens.Add(tokenEntity);
        return refreshToken;
    }

    private async Task<Either<RefreshError, UserEntity>> FetchUserAsync(Guid userId)
    {
        UserEntity? userEntity = await _dataContext.Users
            .FindAsync(userId);
        
        return userEntity is null
            ? RefreshError.UserNotFound
            : userEntity;
    }
}
