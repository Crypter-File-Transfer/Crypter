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

using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Settings;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/settings")]
   public class SettingsController : CrypterController
   {
      private readonly ITokenService _tokenService;
      private readonly IUserAuthenticationService _userAuthenticationService;
      private readonly IUserEmailVerificationService _userEmailVerificationService;
      private readonly IUserService _userService;

      public SettingsController(ITokenService tokenService, IUserAuthenticationService userAuthenticationService, IUserEmailVerificationService userEmailVerificationService, IUserService userService)
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
         var userId = _tokenService.ParseUserId(User);
         var result = await _userService.GetUserSettingsAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpPost("contact-info")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateContactInfoResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(UpdateContactInfoError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               UpdateContactInfoError.UnknownError
                  or UpdateContactInfoError.PasswordHashFailure => ServerError(errorResponse),
               UpdateContactInfoError.UserNotFound => NotFound(errorResponse),
               UpdateContactInfoError.EmailAddressUnavailable => Conflict(errorResponse),
               UpdateContactInfoError.InvalidEmailAddress
                  or UpdateContactInfoError.InvalidPassword
                  or UpdateContactInfoError.PasswordNeedsMigration => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userAuthenticationService.UpdateUserContactInfoAsync(userId, request, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UpdateContactInfoError.UnknownError));
      }

      [HttpPost("profile")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateProfileResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userService.UpdateUserProfileAsync(userId, request, cancellationToken);
         return Ok(result);
      }

      [HttpPost("notification")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateNotificationSettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserNotificationPreferencesAsync([FromBody] UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(UpdateNotificationSettingsError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               UpdateNotificationSettingsError.UnknownError => ServerError(errorResponse),
               UpdateNotificationSettingsError.EmailAddressNotVerified => BadRequest(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userService.UpsertUserNotificationPreferencesAsync(userId, request, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UpdateNotificationSettingsError.UnknownError));
      }

      [HttpPost("privacy")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdatePrivacySettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> UpdateUserPrivacySettingsAsync([FromBody] UpdatePrivacySettingsRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userService.UpsertUserPrivacySettingsAsync(userId, request, cancellationToken);
         return Ok(result);
      }

      [HttpPost("verify")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyEmailAddressResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> VerifyUserEmailAddressAsync([FromBody] VerifyEmailAddressRequest request, CancellationToken cancellationToken)
      {
         var result = await _userEmailVerificationService.VerifyUserEmailAddressAsync(request, cancellationToken);
         return result.Match<IActionResult>(
            () => NotFound(new ErrorResponse(VerifyEmailAddressError.NotFound)),
            x => Ok(new VerifyEmailAddressResponse()));
      }
   }
}
