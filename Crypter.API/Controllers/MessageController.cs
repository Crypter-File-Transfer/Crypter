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

using Crypter.API.Attributes;
using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Transfer;
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
   [Route("api/message")]
   public class MessageController : CrypterController
   {
      private readonly ITransferDownloadService _transferDownloadService;
      private readonly ITransferUploadService _transferUploadService;
      private readonly ITokenService _tokenService;
      private readonly IUserTransferService _userTransferService;

      public MessageController(ITransferDownloadService transferDownloadService, ITransferUploadService transferUploadService, ITokenService tokenService, IUserTransferService userTransferService)
      {
         _transferDownloadService = transferDownloadService;
         _transferUploadService = transferUploadService;
         _tokenService = tokenService;
         _userTransferService = userTransferService;
      }

      [HttpPost]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UploadMessageTransferAsync([FromBody] UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         var uploadResult = await _tokenService.TryParseUserId(User)
            .MatchAsync(
            async () => await _transferUploadService.UploadAnonymousMessageAsync(request, cancellationToken),
            async x => await _transferUploadService.UploadUserMessageAsync(x, Maybe<string>.None, request, cancellationToken));

         return uploadResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UploadTransferError.UnknownError));
      }

      [HttpGet("preview")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetAnonymousMessagePreviewAsync([FromQuery] Guid id, CancellationToken cancellationToken)
      {
         var previewResult = await _transferDownloadService.GetAnonymousMessagePreviewAsync(id, cancellationToken);
         return previewResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(DownloadTransferPreviewError.UnknownError));
      }

      [HttpPost("ciphertext")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferCiphertextResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetAnonymousMessageCiphertextAsync([FromQuery] Guid id, [FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var ciphertextResult = await _transferDownloadService.GetAnonymousMessageCiphertextAsync(id, request, true, cancellationToken);
         return ciphertextResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
      }

      [HttpGet("user/sent")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSentMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetSentMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserSentMessagesAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpGet("user/received")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserReceivedMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> GetReceivedMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userTransferService.GetUserReceivedMessagesAsync(userId, cancellationToken);
         return Ok(result);
      }

      [HttpGet("user/preview")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetUserMessagePreviewAsync([FromQuery] Guid id, CancellationToken cancellationToken)
      {
         var userId = _tokenService.TryParseUserId(User);
         var previewResult = await _transferDownloadService.GetUserMessagePreviewAsync(id, userId, cancellationToken);
         return previewResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(DownloadTransferPreviewError.UnknownError));
      }

      [HttpPost("user/ciphertext")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferCiphertextResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetUserMessageCiphertextAsync([FromQuery] Guid id, [FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.TryParseUserId(User);
         var ciphertextResult = await _transferDownloadService.GetUserMessageCiphertextAsync(id, request, userId, cancellationToken);
         return ciphertextResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
      }
   }
}
