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
using Crypter.API.Contracts;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Monads;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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

      public MessageController(ITransferDownloadService transferDownloadService, ITransferUploadService transferUploadService, ITokenService tokenService)
      {
         _transferDownloadService = transferDownloadService;
         _transferUploadService = transferUploadService;
         _tokenService = tokenService;
      }

      [HttpPost]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UploadMessageTransferAsync([FromForm] UploadMessageTransferReceipt request, CancellationToken cancellationToken)
      {
         using Stream ciphertextStream = request.Ciphertext.OpenReadStream();
         var uploadResult = await _tokenService.TryParseUserId(User)
            .MatchAsync(
            async () => await _transferUploadService.UploadAnonymousMessageAsync(request.Data, ciphertextStream, cancellationToken),
            async x => await _transferUploadService.UploadUserMessageAsync(x, Maybe<string>.None, request.Data, ciphertextStream, cancellationToken));

         return uploadResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(UploadTransferError.UnknownError));
      }

      [HttpGet("preview")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetAnonymousMessagePreviewAsync([FromQuery] string id, CancellationToken cancellationToken)
      {
         var previewResult = await _transferDownloadService.GetAnonymousMessagePreviewAsync(id, cancellationToken);
         return previewResult.Match(
            MakeErrorResponse,
            Ok,
            MakeErrorResponse(DownloadTransferPreviewError.UnknownError));
      }

      [HttpPost("ciphertext")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetAnonymousMessageCiphertextAsync([FromQuery] string id, [FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var ciphertextResult = await _transferDownloadService.GetAnonymousMessageCiphertextAsync(id, request, cancellationToken);
         return ciphertextResult.Match(
            MakeErrorResponse,
            x => new FileStreamResult(x, "application/octet-stream"),
            MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
      }

      [HttpGet("user/preview")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetUserMessagePreviewAsync([FromQuery] string id, CancellationToken cancellationToken)
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
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetUserMessageCiphertextAsync([FromQuery] string id, [FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.TryParseUserId(User);
         var ciphertextResult = await _transferDownloadService.GetUserMessageCiphertextAsync(id, request, userId, cancellationToken);
         return ciphertextResult.Match(
            MakeErrorResponse,
            x => new FileStreamResult(x, "application/octet-stream"),
            MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
      }
   }
}
