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
using Crypter.Core.Interfaces;
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
      private readonly IUserTokenService _userTokenService;

      public AuthenticationController(
         IMediator mediator,
         ITokenService tokenService,
         IUserTokenService userTokenService)
      {
         _mediator = mediator;
         _tokenService = tokenService;
         _userTokenService = userTokenService;
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
         LoginQueryResult loginResult = await _mediator.Send(new LoginQuery(request.Username, request.Password), cancellationToken);
         if (!loginResult.Success)
         {
            return new NotFoundObjectResult(new ErrorResponse(LoginError.NotFound));
         }

         string authToken = _tokenService.NewAuthenticationToken(loginResult.UserId);

         Guid refreshTokenId = Guid.NewGuid();
         (string refreshToken, DateTime sessionTokenExpiration) = request.RefreshTokenType == TokenType.Session
            ? _tokenService.NewSessionToken(loginResult.UserId, refreshTokenId)
            : _tokenService.NewRefreshToken(loginResult.UserId, refreshTokenId);

         string userAgent = HttpContext.Request.Headers.TryGetValue("User-Agent", out var someUserAgent)
            ? someUserAgent.ToString()
            : "Unknown device";
         
         await _mediator.Send(new InsertRefreshTokenCommand(refreshTokenId, loginResult.UserId, userAgent, request.RefreshTokenType, sessionTokenExpiration), cancellationToken);
         BackgroundJob.Schedule(() => _userTokenService.DeleteAsync(refreshTokenId, default), sessionTokenExpiration - DateTime.UtcNow);

         BackgroundJob.Enqueue(() => _mediator.Send(new UpdateLastLoginTimeCommand(loginResult.UserId, DateTime.UtcNow), CancellationToken.None));

         return new OkObjectResult(
             new LoginResponse(loginResult.UserId, authToken, refreshToken, loginResult.EncryptedX25519PrivateKey, loginResult.EncryptedEd25519PrivateKey, loginResult.InitVectorX25519, loginResult.InitVectorEd25519)
         );
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
         if (!_tokenService.TryParseTokenId(User, out var tokenId))
         {
            return new BadRequestObjectResult(new ErrorResponse(RefreshError.BearerTokenMissingId));
         }

         var userToken = await _userTokenService.ReadAsync(tokenId, cancellationToken);
         if (userToken is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(RefreshError.DatabaseTokenNotFound));
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (userToken.Owner != requestingUserId)
         {
            return new NotFoundObjectResult(new ErrorResponse(RefreshError.DatabaseTokenNotFound));
         }

         if (userToken.Expiration < DateTime.UtcNow)
         {
            return new BadRequestObjectResult(new ErrorResponse(RefreshError.DatabaseTokenExpired));
         }

         await _userTokenService.DeleteAsync(userToken.Id, default);

         var newAuthToken = _tokenService.NewAuthenticationToken(requestingUserId);

         var newTokenId = Guid.NewGuid();
         var (singleUseRefreshToken, newTokenExpiration) = userToken.Type == TokenType.Session
            ? _tokenService.NewSessionToken(requestingUserId, newTokenId)
            : _tokenService.NewRefreshToken(requestingUserId, newTokenId);

         HttpContext.Request.Headers.TryGetValue("User-Agent", out var someUserAgent);
         string userAgent = someUserAgent.ToString() ?? "Unknown device";

         await _userTokenService.InsertAsync(newTokenId, requestingUserId, userAgent, userToken.Type, newTokenExpiration, cancellationToken);
         BackgroundJob.Schedule(() => _userTokenService.DeleteAsync(newTokenId, default), newTokenExpiration - DateTime.UtcNow);

         return new OkObjectResult(new RefreshResponse(newAuthToken, singleUseRefreshToken));
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

         var userToken = await _userTokenService.ReadAsync(tokenId, cancellationToken);
         if (userToken == null)
         {
            return new NotFoundObjectResult(new ErrorResponse(LogoutError.DatabaseTokenNotFound));
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (userToken.Owner != requestingUserId)
         {
            return new NotFoundObjectResult(new ErrorResponse(LogoutError.DatabaseTokenNotFound));
         }

         await _userTokenService.DeleteAsync(userToken.Id, cancellationToken);
         return new OkObjectResult(new LogoutResponse());
      }
   }
}
