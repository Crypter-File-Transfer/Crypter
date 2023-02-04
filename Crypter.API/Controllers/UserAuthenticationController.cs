/*
 * Copyright (C) 2023 Crypter File Transfer
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
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Monads;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user/authentication")]
   public class UserAuthenticationController : CrypterControllerBase
   {
      private readonly IUserAuthenticationService _userAuthenticationService;

      public UserAuthenticationController(IUserAuthenticationService userAuthenticationService)
      {
         _userAuthenticationService = userAuthenticationService;
      }

      [HttpPost]
      [Route("register")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(RegistrationError error)
         {
#pragma warning disable CS8524
            return error switch
            {
               RegistrationError.UnknownError
                  or RegistrationError.PasswordHashFailure
                  or RegistrationError.InvalidPasswordConfirm => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
               RegistrationError.InvalidUsername
                  or RegistrationError.InvalidPassword
                  or RegistrationError.InvalidEmailAddress
                  or RegistrationError.OldPasswordVersion => MakeErrorResponseBase(HttpStatusCode.BadRequest, error),
               RegistrationError.UsernameTaken
                  or RegistrationError.EmailAddressTaken => MakeErrorResponseBase(HttpStatusCode.Conflict, error)
            };
#pragma warning restore CS8524
         }

         return await _userAuthenticationService.RegisterAsync(request, cancellationToken)
            .MatchAsync(
               MakeErrorResponse,
               _ => Ok(),
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
#pragma warning disable CS8524
            return error switch
            {
               LoginError.UnknownError
                  or LoginError.PasswordHashFailure => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
               LoginError.InvalidUsername
                  or LoginError.InvalidPassword
                  or LoginError.InvalidTokenTypeRequested
                  or LoginError.ExcessiveFailedLoginAttempts
                  or LoginError.InvalidPasswordVersion => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
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
   }
}
