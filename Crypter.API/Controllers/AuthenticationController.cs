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
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
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
      private readonly ITokenService _tokenService;
      private readonly IUserService _userService;
      private readonly IUserPublicKeyPairService<UserX25519KeyPair> _userX25519KeyPairService;
      private readonly IUserPublicKeyPairService<UserEd25519KeyPair> _userEd25519KeyPairService;
      private readonly IUserTokenService _userTokenService;

      public AuthenticationController(
         ITokenService tokenService,
         IUserService userService,
         IUserPublicKeyPairService<UserX25519KeyPair> userX25519KeyPairService,
         IUserPublicKeyPairService<UserEd25519KeyPair> userEd25519KeyPairService,
         IUserTokenService userTokenService)
      {
         _tokenService = tokenService;
         _userService = userService;
         _userX25519KeyPairService = userX25519KeyPairService;
         _userEd25519KeyPairService = userEd25519KeyPairService;
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
      public async Task<ActionResult<LoginResponse>> AuthenticateAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
      {
         var user = await _userService.AuthenticateAsync(request.Username, request.Password, cancellationToken);
         if (user == null)
         {
            return new NotFoundObjectResult(
               new LoginResponse(default, default, default));
         }

         var authToken = _tokenService.NewAuthenticationToken(user.Id);

         var refreshTokenId = Guid.NewGuid();
         (var refreshToken, var sessionTokenExpiration) = request.RefreshTokenType == TokenType.Session
            ? _tokenService.NewSessionToken(user.Id, refreshTokenId)
            : _tokenService.NewRefreshToken(user.Id, refreshTokenId);

         HttpContext.Request.Headers.TryGetValue("User-Agent", out var someUserAgent);
         string userAgent = someUserAgent.ToString() ?? "Unknown device";
         await _userTokenService.InsertAsync(refreshTokenId, user.Id, userAgent, request.RefreshTokenType, sessionTokenExpiration, cancellationToken);

         var userX25519KeyPair = await _userX25519KeyPairService.GetUserPublicKeyPairAsync(user.Id, cancellationToken);
         var userEd25519KeyPair = await _userEd25519KeyPairService.GetUserPublicKeyPairAsync(user.Id, cancellationToken);

         BackgroundJob.Enqueue(() => _userService.UpdateLastLoginTime(user.Id, DateTime.UtcNow, default));
         BackgroundJob.Schedule(() => _userTokenService.DeleteAsync(refreshTokenId, default), sessionTokenExpiration - DateTime.UtcNow);

         return new OkObjectResult(
             new LoginResponse(user.Id, authToken, refreshToken, userX25519KeyPair?.PrivateKey, userEd25519KeyPair?.PrivateKey, userX25519KeyPair?.ClientIV, userEd25519KeyPair?.ClientIV)
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
      public async Task<ActionResult<RefreshResponse>> RefreshAuthenticationAsync(CancellationToken cancellationToken)
      {
         if (!_tokenService.TryParseTokenId(User, out var tokenId))
         {
            return new BadRequestObjectResult(new RefreshResponse());
         }

         var userToken = await _userTokenService.ReadAsync(tokenId, cancellationToken);
         if (userToken is null)
         {
            return new BadRequestObjectResult(new RefreshResponse());
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (userToken.Owner != requestingUserId || userToken.Expiration < DateTime.UtcNow)
         {
            return new BadRequestObjectResult(new RefreshResponse());
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
      /// <returns></returns>
      [Authorize]
      [HttpPost("logout")]
      public async Task<ActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
      {
         var (tokenIsValid, securityToken) = _tokenService.ValidateToken(request.RefreshToken);
         if (!tokenIsValid
            || securityToken is null)
         {
            return BadRequest();
         }

         if (!_tokenService.TryParseTokenId(securityToken, out var tokenId))
         {
            return BadRequest();
         }

         var userToken = await _userTokenService.ReadAsync(tokenId, cancellationToken);
         if (userToken == null)
         {
            return BadRequest();
         }

         var requestingUserId = _tokenService.ParseUserId(User);
         if (userToken.Owner != requestingUserId)
         {
            return BadRequest();
         }

         await _userTokenService.DeleteAsync(userToken.Id, cancellationToken);
         return Ok();
      }
   }
}
