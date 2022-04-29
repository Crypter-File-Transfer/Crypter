/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.API.Methods;
using Crypter.API.Services;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using Crypter.Contracts.Features.Authentication.Refresh;
using Crypter.Core.Features.User.Commands;
using Crypter.Core.Features.User.Queries;
using Crypter.Core.Models;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/authentication")]
   public class AuthenticationController : ControllerBase
   {
      private readonly IMediator _mediator;
      private readonly ITokenService _tokenService;

      public AuthenticationController(
         IMediator mediator,
         ITokenService tokenService)
      {
         _mediator = mediator;
         _tokenService = tokenService;
      }

      /// <summary>
      /// Handle a login request.
      /// </summary>
      /// <param name="request"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      [HttpPost("login")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> AuthenticateAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
      {
         static IActionResult MakeErrorResponse(LoginError error)
         {
            var errorResponse = new ErrorResponse(error);
            return error switch
            {
               LoginError.NotFound => new NotFoundObjectResult(errorResponse),
               LoginError.InvalidTokenTypeRequested => new BadRequestObjectResult(errorResponse),
               _ => throw new NotImplementedException()
            };
         }

         async Task<Either<LoginError, string>> MakeRefreshTokenAsync(Guid userId, TokenType tokenType, string userAgent)
         {
            var refreshTokenId = Guid.NewGuid();
            Either<LoginError, (string Token, DateTime Expiration)> makeTokenResult = tokenType switch
            {
               TokenType.Session => _tokenService.NewSessionToken(userId, refreshTokenId),
               TokenType.Device => _tokenService.NewRefreshToken(userId, refreshTokenId),
               _ => LoginError.InvalidTokenTypeRequested
            };

            await makeTokenResult.DoRightAsync(async x =>
            {
               await _mediator.Send(new InsertUserTokenCommand(refreshTokenId, userId, userAgent, tokenType, x.Expiration), cancellationToken);
               BackgroundJob.Schedule(() => _mediator.Send(new DeleteUserTokenCommand(refreshTokenId), CancellationToken.None), x.Expiration - DateTime.UtcNow);
            });

            return makeTokenResult.Map(x => x.Token);
         }

         var loginQueryValidation = LoginQuery.ValidateFrom(request.Username, request.Password);
         var loginQueryResult = await loginQueryValidation.BindAsync(async x => await _mediator.Send(x, cancellationToken));

         loginQueryResult.DoRight(x =>
         {
            BackgroundJob.Enqueue(() => _mediator.Send(new UpdateLastLoginTimeCommand(x.UserId, DateTime.UtcNow), CancellationToken.None));
         });

         var response = await loginQueryResult.BindAsync(async loginData =>
         {
            string userAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
            var refreshTokenResult = await MakeRefreshTokenAsync(loginData.UserId, request.RefreshTokenType, userAgent);
            return refreshTokenResult.Map(refreshToken =>
            {
               string authToken = _tokenService.NewAuthenticationToken(loginData.UserId);
               return new LoginResponse(loginData.Username, authToken, refreshToken);
            });
         });

         return response.Match(
            left => MakeErrorResponse(left),
            right => new OkObjectResult(right));
      }

      /// <summary>
      /// Trade in a valid refresh token for a new authentication token and refresh token.
      /// </summary>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      /// <remarks>
      /// This action will accept a valid, un-expired refresh token. In exchange, it will respond with a fresh authentication token
      /// and a fresh refresh token of the same type (i.e. a short-term "session" token vs a long-term "refresh" token).
      /// If the client wants to switch the type of refresh token, it should perform a new login.
      /// The refresh token should be provided in the Authorization header.
      /// </remarks>
      [Authorize]
      [HttpGet("refresh")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RefreshResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> RefreshAuthenticationAsync(CancellationToken cancellationToken)
      {
         static IActionResult MakeErrorResponse(RefreshError error)
         {
            var errorResponse = new ErrorResponse(error);
            return error switch
            {
               RefreshError.BearerTokenMissingId or RefreshError.DatabaseTokenExpired => new BadRequestObjectResult(errorResponse),
               RefreshError.DatabaseTokenNotFound => new NotFoundObjectResult(errorResponse),
               _ => throw new NotImplementedException(),
            };
         }

         static Either<RefreshError, UserToken> ValidateDatabaseToken(UserToken token, Guid userId)
         {
            if (token.Owner != userId)
            {
               return RefreshError.DatabaseTokenNotFound;
            }

            if (token.Expiration < DateTime.UtcNow)
            {
               return RefreshError.DatabaseTokenExpired;
            }

            return token;
         }

         Guid userId = _tokenService.ParseUserId(User);

         var validatedTokenId = _tokenService.TryParseTokenId(User)
            .ToEither(RefreshError.BearerTokenMissingId);

         var validatedDatabaseToken = (await validatedTokenId.BindAsync(
            async tokenId => (await _mediator.Send(new UserTokenQuery(tokenId), cancellationToken))
            .ToEither(RefreshError.DatabaseTokenNotFound)))
            .Bind(databaseToken => ValidateDatabaseToken(databaseToken, userId));

         var response = await validatedDatabaseToken.MapAsync(async databaseToken =>
         {
            await _mediator.Send(new DeleteUserTokenCommand(databaseToken.Id), cancellationToken);

            var newAuthToken = _tokenService.NewAuthenticationToken(userId);

            var newTokenId = Guid.NewGuid();
            (string newRefreshToken, DateTime newTokenExpiration) = databaseToken.Type == TokenType.Device
               ? _tokenService.NewRefreshToken(userId, newTokenId)
               : _tokenService.NewSessionToken(userId, newTokenId);

            string userAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);

            await _mediator.Send(new InsertUserTokenCommand(newTokenId, userId, userAgent, databaseToken.Type, newTokenExpiration), cancellationToken);
            BackgroundJob.Schedule(() => _mediator.Send(new DeleteUserTokenCommand(newTokenId), CancellationToken.None), newTokenExpiration - DateTime.UtcNow);
            return new RefreshResponse(newAuthToken, newRefreshToken, databaseToken.Type);
         });

         return response.Match(
            left => MakeErrorResponse(left),
            right => new OkObjectResult(right));
      }

      /// <summary>
      /// Clears the provided refresh token from the database, ensuring it cannot be used for subsequent requests.
      /// </summary>
      /// <param name="request"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      [Authorize]
      [HttpPost("logout")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LogoutResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
      {
         static IActionResult MakeErrorResponse(LogoutError error)
         {
            var errorResponse = new ErrorResponse(error);
            return error switch
            {
               LogoutError.RefreshTokenInvalid => new BadRequestObjectResult(errorResponse),
               LogoutError.DatabaseTokenNotFound => new NotFoundObjectResult(errorResponse),
               _ => throw new NotImplementedException(),
            };
         }

         Guid userId = _tokenService.ParseUserId(User);

         var validatedClaimsPrincipal = _tokenService.ValidateToken(request.RefreshToken)
            .ToEither(LogoutError.RefreshTokenInvalid);

         var validatedTokenId = validatedClaimsPrincipal.Bind(
            x => _tokenService.TryParseTokenId(x)
            .ToEither(LogoutError.RefreshTokenInvalid));

         var foundUserToken = await validatedTokenId.BindAsync(
            async tokenId => (await _mediator.Send(new UserTokenQuery(tokenId), cancellationToken))
            .ToEither(LogoutError.DatabaseTokenNotFound));

         var response = await foundUserToken.BindAsync<LogoutResponse>(async x =>
         {
            bool isUserTheTokenOwner = userId == x.Owner;
            if (isUserTheTokenOwner)
            {
               await _mediator.Send(new DeleteUserTokenCommand(x.Id), cancellationToken);
            }

            return isUserTheTokenOwner
               ? new LogoutResponse()
               : LogoutError.DatabaseTokenNotFound;
         });

         return response.Match(
            left => MakeErrorResponse(left),
            right => new OkObjectResult(right));
      }
   }
}
