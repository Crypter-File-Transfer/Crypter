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

using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/file/transfer")]
public class FileTransferController : TransferControllerBase
{
    private readonly ISender _sender;
    
    public FileTransferController(ISender sender)
    {
        _sender = sender;
    }
    
    /// <summary>
    /// The ideal way to upload a file of any size.
    /// However, the endpoints to receive chunked files is preferred while this Chromium issue remains unaddressed:
    /// https://issues.chromium.org/issues/339788214
    /// </summary>
    /// <param name="username"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [MaybeAuthorize]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [RequestTimeout(300000)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> UploadFileTransferAsync([FromQuery] string? username,
        [FromForm] UploadFileTransferReceipt request)
    {
        Maybe<string> maybeUsername = string.IsNullOrEmpty(username)
            ? Maybe<string>.None
            : username;
        await using Stream? ciphertextStream = request.Ciphertext?.OpenReadStream();

        SaveFileTransferCommand command = new SaveFileTransferCommand(
            PossibleUserId, maybeUsername, request.Data, ciphertextStream);
        
        return await _sender.Send(command)
            .MatchAsync(
                left: MakeErrorResponse,
                right: Ok,
                neither: MakeErrorResponse(UploadTransferError.UnknownError));
    }

    [HttpGet("received")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserReceivedFileDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetReceivedFilesAsync(CancellationToken cancellationToken)
    {
        UserReceivedFilesQuery request = new UserReceivedFilesQuery(UserId);
        IEnumerable<UserReceivedFileDTO> result = await _sender.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sent")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserSentFileDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetSentFilesAsync(CancellationToken cancellationToken)
    {
        UserSentFilesQuery request = new UserSentFilesQuery(UserId);
        IEnumerable<UserSentFileDTO> result = await _sender.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preview/anonymous")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTransferPreviewResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAnonymousFilePreviewAsync([FromQuery] string id,
        CancellationToken cancellationToken)
    {
        AnonymousFilePreviewQuery request = new AnonymousFilePreviewQuery(id);
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
    public async Task<IActionResult> GetUserFilePreviewAsync([FromQuery] string id, CancellationToken cancellationToken)
    {
        UserFilePreviewQuery request = new UserFilePreviewQuery(id, PossibleUserId);
        return await _sender.Send(request, cancellationToken)
            .MatchAsync(
                left: MakeErrorResponse,
                right: Ok,
                neither: MakeErrorResponse(TransferPreviewError.UnknownError));
    }

    [HttpGet("ciphertext/anonymous")]
    [RequestTimeout(300000)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAnonymousFileCiphertextAsync([FromQuery] string id, [FromQuery] string proof)
    {
        return await DecodeProof(proof)
            .BindAsync(async decodedProof =>
            {
                GetAnonymousFileCiphertextCommand request = new GetAnonymousFileCiphertextCommand(id, decodedProof);
                return await _sender.Send(request);
            })
            .MatchAsync(
                MakeErrorResponse,
                x => new FileStreamResult(x, "application/octet-stream"),
                MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
    }

    [HttpGet("ciphertext/user")]
    [MaybeAuthorize]
    [RequestTimeout(300000)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetUserFileCiphertextAsync([FromQuery] string id, [FromQuery] string proof)
    {
        return await DecodeProof(proof)
            .BindAsync(async decodedProof =>
            {
                GetUserFileCiphertextCommand request =
                    new GetUserFileCiphertextCommand(id, decodedProof, PossibleUserId);
                return await _sender.Send(request);
            })
            .MatchAsync(
                MakeErrorResponse,
                x => new FileStreamResult(x, "application/octet-stream"),
                MakeErrorResponse(DownloadTransferCiphertextError.UnknownError));
    }
}
