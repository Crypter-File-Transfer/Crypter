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

using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers.Old
{
   [ApiController]
   [Route("api/authentication")]
   public class AuthenticationController : CrypterControllerBase
   {
      private readonly ITokenService _tokenService;
      private readonly IUserAuthenticationService _userAuthenticationService;

      public AuthenticationController(ITokenService tokenService, IUserAuthenticationService userAuthenticationService)
      {
         _tokenService = tokenService;
         _userAuthenticationService = userAuthenticationService;
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
#pragma warning disable CS8524
            return error switch
            {
               LogoutError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
               LogoutError.InvalidToken => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
         }

         var logoutResult = await _userAuthenticationService.LogoutAsync(User, cancellationToken);

         return logoutResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(LogoutError.UnknownError));
      }
   }
}
