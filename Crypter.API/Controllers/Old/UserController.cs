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
using Crypter.Common.Contracts.Features.Users;
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
   [Route("api/user")]
   public class UserController : CrypterControllerBase
   {
      private readonly ITokenService _tokenService;
      private readonly IUserService _userService;
      private readonly IUserTransferService _userTransferService;

      public UserController(ITokenService tokenService, IUserService userService, IUserTransferService userTransferService)
      {
         _tokenService = tokenService;
         _userService = userService;
         _userTransferService = userTransferService;
      }

      [HttpGet("{username}/profile")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUserProfileResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetUserProfileAsync(string username, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(GetUserProfileError error)
         {
#pragma warning disable CS8524
            return error switch
            {
               GetUserProfileError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.TryParseUserId(User);
         var result = await _userService.GetUserProfileAsync(userId, username, cancellationToken);
         return result.Match(
            () => MakeErrorResponse(GetUserProfileError.NotFound),
            Ok);
      }

      [HttpGet("self/file/received")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserReceivedFilesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetReceivedFilesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserReceivedFilesAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpGet("self/file/sent")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSentFilesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetSentFilesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserSentFilesAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpGet("self/message/received")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserReceivedMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetReceivedMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserReceivedMessagesAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpGet("self/message/sent")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSentMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetSentMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserSentMessagesAsync(userId, cancellationToken);
         return Ok(result);
      }
   }
}
