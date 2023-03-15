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
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Monads;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user/setting")]
   public class UserSettingController : CrypterControllerBase
   {
      private readonly ITokenService _tokenService;
      private readonly IUserAuthenticationService _userAuthenticationService;
      private readonly IUserEmailVerificationService _userEmailVerificationService;
      private readonly IUserService _userService;

      public UserSettingController(ITokenService tokenService, IUserAuthenticationService userAuthenticationService, IUserEmailVerificationService userEmailVerificationService, IUserService userService)
      {
         _tokenService = tokenService;
         _userAuthenticationService = userAuthenticationService;
         _userEmailVerificationService = userEmailVerificationService;
         _userService = userService;
      }

      [HttpGet]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetUserSettingsAsync(CancellationToken cancellationToken)
      {
         Guid userId = _tokenService.ParseUserId(User);
         UserSettingsResponse result = await _userService.GetUserSettingsAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpPost("contact")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(UpdateContactInfoError error)
         {
#pragma warning disable CS8524
            return error switch
            {
               UpdateContactInfoError.UnknownError
                  or UpdateContactInfoError.PasswordHashFailure => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
               UpdateContactInfoError.UserNotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error),
               UpdateContactInfoError.EmailAddressUnavailable => MakeErrorResponseBase(HttpStatusCode.Conflict, error),
               UpdateContactInfoError.InvalidEmailAddress
                  or UpdateContactInfoError.InvalidPassword
                  or UpdateContactInfoError.PasswordNeedsMigration => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
         }

         Guid userId = _tokenService.ParseUserId(User);
         return await _userAuthenticationService.UpdateUserContactInfoAsync(userId, request, cancellationToken)
            .MatchAsync(
               MakeErrorResponse,
               _ => Ok(),
               MakeErrorResponse(UpdateContactInfoError.UnknownError));
      }
   }
}
