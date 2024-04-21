﻿/*
 * Copyright (C) 2024 Crypter File Transfer
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Features.Transfer.Events;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.DataAccess;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer.Queries;

public sealed record AnonymousMessagePreviewQuery(string HashId)
    : IEitherRequest<TransferPreviewError, MessageTransferPreviewResponse>;

internal class AnonymousMessagePreviewQueryHandler
    : IEitherRequestHandler<AnonymousMessagePreviewQuery, TransferPreviewError, MessageTransferPreviewResponse>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;

    public AnonymousMessagePreviewQueryHandler(DataContext dataContext, IHashIdService hashIdService, IPublisher publisher, ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferRepository = transferRepository;
    }
    
    public async Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> Handle(AnonymousMessagePreviewQuery request, CancellationToken cancellationToken)
    {
        Guid itemId = _hashIdService.Decode(request.HashId);
        return await GetAnonymousMessagePreviewAsync(itemId)
            .DoRightAsync(
            async _ =>
            {
                SuccessfulTransferPreviewEvent successfulTransferPreviewEvent =
                    new SuccessfulTransferPreviewEvent(itemId, TransferItemType.Message, null, DateTimeOffset.UtcNow);
                await _publisher.Publish(successfulTransferPreviewEvent, CancellationToken.None);
            })
            .DoLeftOrNeitherAsync(
                async error =>
                {
                    FailedTransferPreviewEvent failedTransferPreviewEvent =
                        new FailedTransferPreviewEvent(itemId, TransferItemType.Message, null, error, DateTimeOffset.UtcNow);
                    await _publisher.Publish(failedTransferPreviewEvent, CancellationToken.None);
                },
                async () =>
                {
                    FailedTransferPreviewEvent failedTransferPreviewEvent =
                        new FailedTransferPreviewEvent(itemId, TransferItemType.Message, null, TransferPreviewError.UnknownError, DateTimeOffset.UtcNow);
                    await _publisher.Publish(failedTransferPreviewEvent, CancellationToken.None);
                });
    }

    private async Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetAnonymousMessagePreviewAsync(Guid itemId)
    {
        MessageTransferPreviewResponse? messagePreview = await _dataContext.AnonymousMessageTransfers
            .Where(x => x.Id == itemId)
            .Select(x => new MessageTransferPreviewResponse(
                x.Subject,
                x.Size, 
                string.Empty, 
                string.Empty, 
                string.Empty,
                x.PublicKey,
                x.KeyExchangeNonce, 
                x.Created,
                x.Expiration))
            .FirstOrDefaultAsync(CancellationToken.None);

        if (messagePreview is null)
        {
            return TransferPreviewError.NotFound;
        }
        
        bool ciphertextExists = _transferRepository
            .TransferExists(itemId, TransferItemType.Message, TransferUserType.Anonymous);

        if (!ciphertextExists)
        {
            return TransferPreviewError.NotFound;
        }

        return messagePreview;
    }
}
