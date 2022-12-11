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
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Authentication;
using Crypter.Core.Services;
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
   public class AuthenticationController : CrypterController
   {
      private readonly ITokenService _tokenService;
      private readonly IUserAuthenticationService _userAuthenticationService;

      public AuthenticationController(ITokenService tokenService, IUserAuthenticationService userAuthenticationService)
      {
         _tokenService = tokenService;
         _userAuthenticationService = userAuthenticationService;
      }

      /// <summary>
      /// Handle a user registration request.
      /// </summary>
      /// <param name="request"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      [HttpPost("register")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegistrationResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(RegistrationError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               RegistrationError.UnknownError
                  or RegistrationError.PasswordHashFailure
                  or RegistrationError.InvalidPasswordConfirm => ServerError(errorResponse),
               RegistrationError.InvalidUsername
                  or RegistrationError.InvalidPassword
                  or RegistrationError.InvalidEmailAddress
                  or RegistrationError.OldPasswordVersion => BadRequest(errorResponse),
               RegistrationError.UsernameTaken
                  or RegistrationError.EmailAddressTaken => Conflict(errorResponse)
            };
#pragma warning restore CS8524
         }

         var registrationResult = await _userAuthenticationService.RegisterAsync(request, cancellationToken);

         return registrationResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(RegistrationError.UnknownError));
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
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(LoginError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               LoginError.UnknownError
                  or LoginError.PasswordHashFailure => ServerError(errorResponse),
               LoginError.InvalidUsername
                  or LoginError.InvalidPassword
                  or LoginError.InvalidTokenTypeRequested
                  or LoginError.ExcessiveFailedLoginAttempts
                  or LoginError.InvalidPasswordVersion => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         var requestUserAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
         var loginResult = await _userAuthenticationService.LoginAsync(request, requestUserAgent, cancellationToken);
         return loginResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(LoginError.UnknownError));
      }

      /// <summary>
      /// Trade in a valid refresh token for a new authentication token and refresh token.
      /// </summary>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      /// <remarks>
      /// This action will accept a valid, un-expired refresh token. In exchange, it will respond with a fresh authentication token
      /// and a fresh refresh token of the same type (i.e. a short-term "session" token vs a long-term "device" token).
      /// If the client wants to switch the type of refresh token, it should perform a new login.
      /// The refresh token should be provided in the Authorization header.
      /// </remarks>
      [HttpGet("refresh")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RefreshResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(RefreshError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               RefreshError.UnknownError => ServerError(errorResponse),
               RefreshError.UserNotFound => NotFound(errorResponse),
               RefreshError.InvalidToken => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         var requestUserAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
         var refreshResult = await _userAuthenticationService.RefreshAsync(User, requestUserAgent, cancellationToken);

         return refreshResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(RefreshError.UnknownError));
      }

      /// <summary>
      /// Clears the provided refresh token from the database, ensuring it cannot be used for subsequent requests.
      /// </summary>
      /// <param name="request"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      /// <remarks>
      /// The refresh token should be provided in the Authorization header.
      /// </remarks>
      [HttpPost("logout")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LogoutResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> Logout(CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(LogoutError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               LogoutError.UnknownError => ServerError(errorResponse),
               LogoutError.InvalidToken => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         var logoutResult = await _userAuthenticationService.LogoutAsync(User, cancellationToken);

         return logoutResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(LogoutError.UnknownError));
      }

      [HttpPost("password/test")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestPasswordResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> TestPasswordAsync([FromBody] TestPasswordRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(TestPasswordError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               TestPasswordError.UnknownError
                  or TestPasswordError.PasswordHashFailure => ServerError(errorResponse),
               TestPasswordError.InvalidPassword
                  or TestPasswordError.PasswordNeedsMigration => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         Guid userId = _tokenService.ParseUserId(User);
         var testPasswordResult = await _userAuthenticationService.TestUserPasswordAsync(userId, request, cancellationToken);
         return testPasswordResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(TestPasswordError.UnknownError));
      }
   }
}
