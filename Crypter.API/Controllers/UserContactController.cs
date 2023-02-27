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
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Contacts.RequestErrorCodes;
using Crypter.Common.Monads;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user/contact")]
   public class UserContactController : CrypterControllerBase
   {
      private readonly IUserContactsService _userContactsService;
      private readonly ITokenService _tokenService;

      public UserContactController(IUserContactsService userContactsService, ITokenService tokenService)
      {
         _userContactsService = userContactsService;
         _tokenService = tokenService;
      }

      /// <summary>
      /// Get a list of user contacts.
      /// </summary>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      [HttpGet]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserContact>))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetUserContactsAsync(CancellationToken cancellationToken)
      {
         Guid userId = _tokenService.ParseUserId(User);
         List<UserContact> result = await _userContactsService.GetUserContactsAsync(userId, cancellationToken);
         return Ok(result);
      }

      /// <summary>
      /// Add a user as a contact.
      /// </summary>
      /// <param name="username"></param>
      /// <returns></returns>
      [HttpPost]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserContact))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> AddUserContactAsync([FromQuery] string username)
      {
         IActionResult MakeErrorResponse(AddUserContactError error)
         {
#pragma warning disable CS8524
            return error switch
            {
               AddUserContactError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
               AddUserContactError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error),
               AddUserContactError.InvalidUser => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
         }

         Guid userId = _tokenService.ParseUserId(User);
         return await _userContactsService.UpsertUserContactAsync(userId, username)
            .MatchAsync(
               MakeErrorResponse,
               Ok,
               MakeErrorResponse(AddUserContactError.UnknownError));
      }

      /// <summary>
      /// Remove a user from contacts.
      /// </summary>
      /// <param name="username"></param>
      /// <returns></returns>
      [HttpDelete]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> RemoveUserContactAsync([FromQuery] string username)
      {
         Guid userId = _tokenService.ParseUserId(User);
         await _userContactsService.RemoveUserContactAsync(userId, username);
         return Ok();
      }
   }
}
