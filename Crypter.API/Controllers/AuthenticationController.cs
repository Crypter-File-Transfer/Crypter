/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.API.Services;
using Crypter.Contracts.Common;
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using Crypter.Contracts.Features.Authentication.Refresh;
using Crypter.Core.Features.User.Commands;
using Crypter.Core.Features.User.Queries;
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
      /// <remarks>
      /// The primary responsibility of this action is to verify the provided user credentials against information stored in the database.
      /// If the user credentials are valid, respond with various things the client needs to be useful, including (but not limited to):
      ///  * An authentication token, good for a short amount of time. Authentication tokens are good for multiple requests as long as they have not expired.
      ///  * A refresh token, good for one request only. The two types of refresh tokens are "session" and "refresh" tokens. Session tokens expire
      ///    relatively quickly. Refresh tokens are good for significantly longer periods of time. The client may ask for either. The client should not ask for both.
      ///  * The user's encrypted, private keys. The client will need to decrypt these.
      ///  * The initialization vectors used to encrypt/decrypt the user's private keys.
      /// </remarks>
      [HttpPost("login")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> AuthenticateAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
      {
         if (request.RefreshTokenType != TokenType.Session
            && request.RefreshTokenType != TokenType.Refresh)
         {
            return BadRequest(new ErrorResponse(LoginError.InvalidTokenTypeRequested));
         }

         var result = await _mediator.Send(new LoginQuery(request.Username, request.Password), cancellationToken);

         string authToken = result.Match(
            left => null,
            right => _tokenService.NewAuthenticationToken(right.UserId));

         (Guid refreshTokenId, string refreshToken, DateTime refreshTokenExpiration) = result.Match(
            left => (Guid.Empty, null, default),
            right =>
            {
               Guid id = Guid.NewGuid();
               (string token, DateTime expiration) = request.RefreshTokenType == TokenType.Refresh
                  ? _tokenService.NewRefreshToken(right.UserId, id)
                  : _tokenService.NewSessionToken(right.UserId, id);

               return (id, token, expiration);
            });

         await result.DoRightAsync(async x =>
         {
            HttpContext.Request.Headers.TryGetValue("User-Agent", out var someUserAgent);
            string userAgent = someUserAgent.ToString() ?? "Unknown device";

            await _mediator.Send(new InsertUserTokenCommand(refreshTokenId, x.UserId, userAgent, request.RefreshTokenType, refreshTokenExpiration), cancellationToken);
            BackgroundJob.Schedule(() => _mediator.Send(new DeleteUserTokenCommand(refreshTokenId), CancellationToken.None), refreshTokenExpiration - DateTime.UtcNow);
            BackgroundJob.Enqueue(() => _mediator.Send(new UpdateLastLoginTimeCommand(x.UserId, DateTime.UtcNow), CancellationToken.None));
         });

         return result.Match<IActionResult>(
            left => new NotFoundObjectResult(new ErrorResponse(left)),
            right => new OkObjectResult(new LoginResponse(right.UserId, authToken, refreshToken, right.EncryptedX25519PrivateKey, right.EncryptedEd25519PrivateKey, right.InitVectorX25519, right.InitVectorEd25519)));
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
         if (!_tokenService.TryParseTokenId(User, out Guid tokenId))
         {
            return new BadRequestObjectResult(new ErrorResponse(RefreshError.BearerTokenMissingId));
         }

         var databaseToken = await _mediator.Send(new UserTokenQuery(tokenId), cancellationToken);
         if (databaseToken is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(RefreshError.DatabaseTokenNotFound));
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (databaseToken.Owner != requestingUserId)
         {
            return new NotFoundObjectResult(new ErrorResponse(RefreshError.DatabaseTokenNotFound));
         }

         if (databaseToken.Expiration < DateTime.UtcNow)
         {
            return new BadRequestObjectResult(new ErrorResponse(RefreshError.DatabaseTokenExpired));
         }

         await _mediator.Send(new DeleteUserTokenCommand(databaseToken.Id), cancellationToken);

         var newAuthToken = _tokenService.NewAuthenticationToken(requestingUserId);

         var newTokenId = Guid.NewGuid();
         (string newRefreshToken, DateTime newTokenExpiration) = databaseToken.Type == TokenType.Refresh
            ? _tokenService.NewRefreshToken(requestingUserId, newTokenId)
            : _tokenService.NewSessionToken(requestingUserId, newTokenId);

         HttpContext.Request.Headers.TryGetValue("User-Agent", out var someUserAgent);
         string userAgent = someUserAgent.ToString() ?? "Unknown device";

         await _mediator.Send(new InsertUserTokenCommand(newTokenId, requestingUserId, userAgent, databaseToken.Type, newTokenExpiration), cancellationToken);
         BackgroundJob.Schedule(() => _mediator.Send(new DeleteUserTokenCommand(newTokenId), CancellationToken.None), newTokenExpiration - DateTime.UtcNow);

         return new OkObjectResult(new RefreshResponse(newAuthToken, newRefreshToken));
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
         var (tokenIsValid, claimsPrincipal) = _tokenService.ValidateToken(request.RefreshToken);
         if (!tokenIsValid
            || claimsPrincipal is null)
         {
            return new BadRequestObjectResult(new ErrorResponse(LogoutError.RefreshTokenInvalid));
         }

         if (!_tokenService.TryParseTokenId(claimsPrincipal, out var tokenId))
         {
            return new BadRequestObjectResult(new ErrorResponse(LogoutError.RefreshTokenInvalid));
         }

         var userToken = await _mediator.Send(new UserTokenQuery(tokenId), cancellationToken);
         if (userToken == null)
         {
            return new NotFoundObjectResult(new ErrorResponse(LogoutError.DatabaseTokenNotFound));
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (userToken.Owner != requestingUserId)
         {
            return new NotFoundObjectResult(new ErrorResponse(LogoutError.DatabaseTokenNotFound));
         }

         await _mediator.Send(new DeleteUserTokenCommand(userToken.Id), cancellationToken);
         return new OkObjectResult(new LogoutResponse());
      }
   }
}
