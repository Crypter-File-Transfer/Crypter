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
using System.Threading;
using System.Threading.Tasks;
using Crypter.API.Attributes;
using Crypter.API.Contracts;
using Crypter.API.Controllers.Base;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Core.Features.Transfer.Commands;
using Crypter.Core.Features.Transfer.Queries;
using EasyMonads;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/message/transfer")]
public class MessageTransferController : TransferControllerBase
{
    private readonly ISender _sender;
    
    public MessageTransferController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [MaybeAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> UploadMessageTransferAsync([FromQuery] string? username,
        [FromForm] UploadMessageTransferReceipt request)
    {
        Maybe<string> maybeUsername = string.IsNullOrEmpty(username)
            ? Maybe<string>.None
            : username;

        SaveMessageTransferCommand command = new SaveMessageTransferCommand(
            PossibleUserId, maybeUsername, request.Data, request.Ciphertext);
        
        return await _sender.Send(command)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(UploadTransferError.UnknownError));
    }

    [HttpGet("received")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserReceivedMessageDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetReceivedMessagesAsync(CancellationToken cancellationToken)
    {
        UserReceivedMessagesQuery request = new UserReceivedMessagesQuery(UserId);
        IEnumerable<UserReceivedMessageDTO> result = await _sender.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sent")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserSentMessageDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetSentMessagesAsync(CancellationToken cancellationToken)
    {
        UserSentMessagesQuery request = new UserSentMessagesQuery(UserId);
        IEnumerable<UserSentMessageDTO> result = await _sender.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preview/anonymous")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTransferPreviewResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAnonymousMessagePreviewAsync([FromQuery] string id,
        CancellationToken cancellationToken)
    {
        AnonymousMessagePreviewQuery request = new AnonymousMessagePreviewQuery(id);
        return await _sender.Send(request, cancellationToken)
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
        UserMessagePreviewQuery request = new UserMessagePreviewQuery(id, PossibleUserId);
        return await _sender.Send(request, cancellationToken)
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
            {
                GetAnonymousMessageCiphertextCommand request = new GetAnonymousMessageCiphertextCommand(id, decodedProof);
                return await _sender.Send(request);
            })
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
            {
                GetUserMessageCiphertextCommand request =
                    new GetUserMessageCiphertextCommand(id, decodedProof, PossibleUserId);
                return await _sender.Send(request);
            })
            .MatchAsync(
                MakeErrorResponse,
                x => new FileStreamResult(x, "application/octet-stream"),
                MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
    }
}
