/*
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Features.Transfer.Events;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record GetUserMessageCiphertextCommand(string HashId, byte[] Proof, Maybe<Guid> RequestorId)
    : IEitherRequest<DownloadTransferCiphertextError, FileStream>;

internal class GetUserMessageCiphertextCommandHandler
    : IEitherRequestHandler<GetUserMessageCiphertextCommand, DownloadTransferCiphertextError, FileStream>
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;

    private bool _transferHasRecipient;
    
    public GetUserMessageCiphertextCommandHandler(
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IHashIdService hashIdService,
        IPublisher publisher,
        ITransferRepository transferRepository)
    {
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferRepository = transferRepository;
    }

    public async Task<Either<DownloadTransferCiphertextError, FileStream>> Handle(GetUserMessageCiphertextCommand request, CancellationToken cancellationToken)
    {
        Guid? itemId = _hashIdService.Decode(request.HashId)
            .Match((Guid?)null, x => x);
        
        if (!itemId.HasValue)
        {
            return DownloadTransferCiphertextError.NotFound;
        }
        
        Guid? nullableRequesterUserId = request.RequestorId
            .Match<Guid?>(() => null, x => x);
        
        return await GetFileStreamAsync(itemId.Value, nullableRequesterUserId, request.Proof)
            .DoRightAsync(async _ =>
            {
                SuccessfulTransferDownloadEvent downloadEvent = new SuccessfulTransferDownloadEvent(itemId.Value, TransferItemType.Message, TransferUserType.User, nullableRequesterUserId, !_transferHasRecipient, DateTimeOffset.UtcNow);
                await _publisher.Publish(downloadEvent, CancellationToken.None);
            })
            .DoLeftOrNeitherAsync(
                async error =>
                {
                    FailedTransferDownloadEvent failedTransferDownloadEvent = new FailedTransferDownloadEvent(itemId.Value, TransferItemType.Message, nullableRequesterUserId, error, DateTimeOffset.UtcNow);
                    await _publisher.Publish(failedTransferDownloadEvent, CancellationToken.None);
                },
                async () =>
                {
                    FailedTransferDownloadEvent failedTransferDownloadEvent = new FailedTransferDownloadEvent(itemId.Value, TransferItemType.Message, nullableRequesterUserId, DownloadTransferCiphertextError.UnknownError, DateTimeOffset.UtcNow);
                    await _publisher.Publish(failedTransferDownloadEvent, CancellationToken.None);
                });
    }

    private async Task<Either<DownloadTransferCiphertextError, FileStream>> GetFileStreamAsync(Guid itemId, Guid? requesterId, byte[] requestProof)
    {
        var databaseData = await _dataContext.UserMessageTransfers
            .Where(x => x.Id == itemId)
            .Where(x => x.RecipientId == null || x.RecipientId == requesterId)
            .Select(x => new { x.RecipientId, x.Proof })
            .FirstOrDefaultAsync(CancellationToken.None);
        
        if (databaseData is null)
        {
            return DownloadTransferCiphertextError.NotFound;
        }
        
        bool ciphertextExists = _transferRepository.TransferExists(itemId, TransferItemType.Message, TransferUserType.User);
        if (!ciphertextExists)
        {
            return DownloadTransferCiphertextError.NotFound;
        }
        
        if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, requestProof))
        {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
        }
        
        _transferHasRecipient = !databaseData.RecipientId.HasValue;
        return _transferRepository
            .GetTransfer(itemId, TransferItemType.Message, TransferUserType.User, !_transferHasRecipient)
            .ToEither(DownloadTransferCiphertextError.NotFound);
    }
}
