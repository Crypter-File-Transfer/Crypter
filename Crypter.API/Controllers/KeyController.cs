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
using Crypter.Contracts.Features.Keys;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/keys")]
   public class KeyController : CrypterController
   {
      private readonly IUserKeysService _userKeysService;
      private readonly ITokenService _tokenService;

      public KeyController(IUserKeysService userKeysService, ITokenService tokenService)
      {
         _userKeysService = userKeysService;
         _tokenService = tokenService;
      }

      [HttpGet("master")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetMasterKeyResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetMasterKeyAsync(CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(GetMasterKeyError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               GetMasterKeyError.UnknownError => ServerError(errorResponse),
               GetMasterKeyError.NotFound => NotFound(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userKeysService.GetMasterKeyAsync(userId, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(GetMasterKeyError.UnknownError));
      }

      [HttpPut("master")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpsertMasterKeyResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]


      [HttpGet("diffie-hellman/private")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetPrivateKeyResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetDiffieHellmanPrivateKeyAsync(CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(GetPrivateKeyError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               GetPrivateKeyError.UnkownError => ServerError(errorResponse),
               GetPrivateKeyError.NotFound => NotFound(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userKeysService.GetDiffieHellmanPrivateKeyAsync(userId, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(GetPrivateKeyError.UnkownError));
      }

      [HttpPut("diffie-hellman")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpsertKeyPairResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpsertDiffieHellmanKeysAsync([FromBody] UpsertKeyPairRequest body, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(UpsertKeyPairError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               UpsertKeyPairError.UnknownError => ServerError(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userKeysService.UpsertDiffieHellmanKeyPairAsync(userId, body, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UpsertKeyPairError.UnknownError));
      }

      [HttpGet("digital-signature/private")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetPrivateKeyResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetDigitalSignaturePrivateKeyAsync(CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(GetPrivateKeyError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               GetPrivateKeyError.UnkownError => ServerError(errorResponse),
               GetPrivateKeyError.NotFound => NotFound(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userKeysService.GetDigitalSignaturePrivateKeyAsync(userId, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(GetPrivateKeyError.UnkownError));
      }

      [HttpPut("digital-signature")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpsertKeyPairResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpsertDigitalSignatureKeysAsync([FromBody] UpsertKeyPairRequest body, CancellationToken cancellationToken)
      {
         IActionResult MakeErrorResponse(UpsertKeyPairError error)
         {
            var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
            return error switch
            {
               UpsertKeyPairError.UnknownError => ServerError(errorResponse)
            };
#pragma warning restore CS8524
         }

         var userId = _tokenService.ParseUserId(User);
         var result = await _userKeysService.UpsertDigitalSignatureKeyPairAsync(userId, body, cancellationToken);
         return result.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UpsertKeyPairError.UnknownError));
      }
   }
}
