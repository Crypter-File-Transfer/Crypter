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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Crypter.API.Attributes;
using Crypter.API.Contracts;
using Crypter.API.Controllers.Base;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Core.Services;
using EasyMonads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/message/transfer")]
public class MessageTransferController : TransferControllerBase
{
    public MessageTransferController(ITransferDownloadService transferDownloadService,
        ITransferUploadService transferUploadService, IUserTransferService userTransferService)
        : base(transferDownloadService, transferUploadService, userTransferService)
    {
    }

    [HttpPost]
    [MaybeAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> UploadMessageTransferAsync([FromQuery] string username,
        [FromForm] UploadMessageTransferReceipt request)
    {
        Maybe<string> maybeUsername = string.IsNullOrEmpty(username)
            ? Maybe<string>.None
            : username;
        await using Stream ciphertextStream = request.Ciphertext.OpenReadStream();

        return await TransferUploadService
            .UploadMessageTransferAsync(PossibleUserId, maybeUsername, request.Data, ciphertextStream)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(UploadTransferError.UnknownError));
    }

    [HttpGet("received")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserReceivedMessageDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetReceivedMessagesAsync(CancellationToken cancellationToken)
    {
        List<UserReceivedMessageDTO> result =
            await UserTransferService.GetUserReceivedMessagesAsync(UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sent")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserSentMessageDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetSentMessagesAsync(CancellationToken cancellationToken)
    {
        List<UserSentMessageDTO>
            result = await UserTransferService.GetUserSentMessagesAsync(UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preview/anonymous")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTransferPreviewResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAnonymousMessagePreviewAsync([FromQuery] string id,
        CancellationToken cancellationToken)
    {
        return await TransferDownloadService.GetAnonymousMessagePreviewAsync(id, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(TransferPreviewError.UnknownError));
    }

    [HttpGet("preview/user")]
    [MaybeAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTransferPreviewResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetUserMessagePreviewAsync([FromQuery] string id,
        CancellationToken cancellationToken)
    {
        return await TransferDownloadService.GetUserMessagePreviewAsync(id, PossibleUserId, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(TransferPreviewError.UnknownError));
    }

    [HttpGet("ciphertext/anonymous")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAnonymousMessageCiphertextAsync([FromQuery] string id, [FromQuery] string proof)
    {
        return await DecodeProof(proof)
            .BindAsync(async decodedProof =>
                await TransferDownloadService.GetAnonymousMessageCiphertextAsync(id, decodedProof))
            .MatchAsync(
                MakeErrorResponse,
                x => new FileStreamResult(x, "application/octet-stream"),
                MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
    }

    [HttpGet("ciphertext/user")]
    [MaybeAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetUserMessageCiphertextAsync([FromQuery] string id, [FromQuery] string proof)
    {
        return await DecodeProof(proof)
            .BindAsync(async decodedProof =>
                await TransferDownloadService.GetUserMessageCiphertextAsync(id, decodedProof, PossibleUserId))
            .MatchAsync(
                MakeErrorResponse,
                x => new FileStreamResult(x, "application/octet-stream"),
                MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
    }
}
