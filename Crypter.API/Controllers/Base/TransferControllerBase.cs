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
using System.Net;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Infrastructure;
using Crypter.Core.Services;
using EasyMonads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers.Base;

public abstract class TransferControllerBase : CrypterControllerBase
{
    protected readonly ISender Sender;
    protected readonly ITransferUploadService TransferUploadService;
    protected readonly IUserTransferService UserTransferService;

    protected TransferControllerBase(
        ISender sender,
        ITransferUploadService transferUploadService,
        IUserTransferService userTransferService)
    {
        Sender = sender;
        TransferUploadService = transferUploadService;
        UserTransferService = userTransferService;
    }

    protected static Either<DownloadTransferCiphertextError, byte[]> DecodeProof(string base64EncodedProof)
    {
        try
        {
            return UrlSafeEncoder.DecodeBytesFromUrlSafe(base64EncodedProof);
        }
        catch (Exception)
        {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
        }
    }

    protected IActionResult MakeErrorResponse(UploadTransferError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            UploadTransferError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
            UploadTransferError.InvalidRequestedLifetimeHours
                or UploadTransferError.OutOfSpace => MakeErrorResponseBase(HttpStatusCode.BadRequest, error),
            UploadTransferError.RecipientNotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
        };
#pragma warning restore CS8524
    }

    protected IActionResult MakeErrorResponse(TransferPreviewError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            TransferPreviewError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
            TransferPreviewError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
        };
#pragma warning restore CS8524
    }

    protected IActionResult MakeErrorResponse(DownloadTransferCiphertextError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            DownloadTransferCiphertextError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError,
                error),
            DownloadTransferCiphertextError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error),
            DownloadTransferCiphertextError.InvalidRecipientProof => MakeErrorResponseBase(HttpStatusCode.BadRequest,
                error)
        };
#pragma warning restore CS8524
    }
}
