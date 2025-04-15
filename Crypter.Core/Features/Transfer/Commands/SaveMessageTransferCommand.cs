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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Features.Transfer.Events;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record SaveMessageTransferCommand(
    Maybe<Guid> SenderId,
    Maybe<string> RecipientUsername,
    UploadMessageTransferRequest? Request,
    Stream? CiphertextStream)
    : IEitherRequest<UploadTransferError, UploadTransferResponse>;

internal class SaveMessageTransferCommandHandler
    : IEitherRequestHandler<SaveMessageTransferCommand, UploadTransferError, UploadTransferResponse>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;
    private readonly TransferStorageSettings _transferStorageSettings;

    public SaveMessageTransferCommandHandler(
        DataContext dataContext,
        IHashIdService hashIdService,
        IPublisher publisher,
        ITransferRepository transferRepository,
        IOptions<TransferStorageSettings> transferStorageSettings)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferRepository = transferRepository;
        _transferStorageSettings = transferStorageSettings.Value;
    }

    public async Task<Either<UploadTransferError, UploadTransferResponse>> Handle(SaveMessageTransferCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset eventTimestamp = DateTimeOffset.UtcNow;
        Task<Either<UploadTransferError, UploadTransferResponse>> responseTask;

        if (request.Request is null || request.CiphertextStream is null)
        {
            responseTask = Either<UploadTransferError, UploadTransferResponse>
                .FromLeft(UploadTransferError.UnknownError)
                .AsTask();
        }
        else
        {
            responseTask =
                from recipientId in Common.ValidateTransferUploadAsync(
                    _dataContext,
                    request.SenderId,
                    request.RecipientUsername,
                    TransferItemType.Message,
                    request.Request.LifetimeHours,
                    request.CiphertextStream.Length)
                let newTransferId = Guid.NewGuid()
                let transferUserType = Common.DetermineUploadTransferUserType(request.SenderId, recipientId)
                from response in SaveMessageTransferAsync(
                    newTransferId,
                    transferUserType,
                    request.SenderId,
                    recipientId,
                    request.Request,
                    request.CiphertextStream)
                let successfulUploadEvent = new SuccessfulTransferUploadEvent(
                    newTransferId, 
                    TransferItemType.Message,
                    transferUserType,
                    request.CiphertextStream.Length,
                    request.SenderId,
                    recipientId,
                    request.RecipientUsername,
                    response.ExpirationUTC,
                    eventTimestamp)
                from sideEffects in Either<UploadTransferError, Unit>.FromRightAsync(
                    UnitPublisher.Publish(_publisher, successfulUploadEvent))
                select response;
        }
        
        return await responseTask
            .DoLeftOrNeitherAsync(
                async error =>
                {
                    FailedTransferUploadEvent failedUploadEvent = new FailedTransferUploadEvent(TransferItemType.Message, error, request.SenderId, request.RecipientUsername, eventTimestamp);
                    await _publisher.Publish(failedUploadEvent, CancellationToken.None);
                },
                async () =>
                {
                    FailedTransferUploadEvent failedUploadEvent = new FailedTransferUploadEvent(TransferItemType.Message, UploadTransferError.UnknownError, request.SenderId, request.RecipientUsername, eventTimestamp);
                    await _publisher.Publish(failedUploadEvent, CancellationToken.None);
                });
    }
    
    private async Task<Either<UploadTransferError, UploadTransferResponse>> SaveMessageTransferAsync(
        Guid newTransferId,
        TransferUserType transferUserType,
        Maybe<Guid> senderId,
        Maybe<Guid> recipientId,
        UploadMessageTransferRequest request,
        Stream ciphertextStream)
    {
        bool storageSuccess = await _transferRepository.SaveTransferAsync(
            newTransferId,
            TransferItemType.Message,
            transferUserType,
            ciphertextStream);
                
        if (!storageSuccess)
        {
            return UploadTransferError.UnknownError;
        }

        return await SaveMessageTransferToDatabaseAsync(
            newTransferId,
            senderId,
            recipientId,
            ciphertextStream.Length,
            request,
            transferUserType);
    }
    
    private async Task<UploadTransferResponse> SaveMessageTransferToDatabaseAsync(
        Guid transferId,
        Maybe<Guid> senderId,
        Maybe<Guid> recipientId,
        long requiredDiskSpace,
        UploadMessageTransferRequest request,
        TransferUserType transferUserType)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime utcExpiration = utcNow.AddHours(request.LifetimeHours);
        
        Guid? nullableSenderId = senderId.Match<Guid?>(() => null,x  => x);
        Guid? nullableRecipientId = recipientId.Match<Guid?>(() => null,x  => x);

        if (senderId.IsNone && recipientId.IsNone)
        {
            AnonymousMessageTransferEntity transferEntity = new AnonymousMessageTransferEntity(
                id: transferId,
                size: requiredDiskSpace,
                publicKey: request.PublicKey!,
                keyExchangeNonce: request.KeyExchangeNonce,
                proof: request.Proof,
                created: utcNow,
                expiration: utcExpiration,
                subject: request.Subject);
            _dataContext.AnonymousMessageTransfers.Add(transferEntity);
        }
        else
        {
            UserMessageTransferEntity transferEntity = new UserMessageTransferEntity(
                id: transferId,
                size: requiredDiskSpace,
                publicKey: request.PublicKey,
                keyExchangeNonce: request.KeyExchangeNonce,
                proof: request.Proof,
                created: utcNow,
                expiration: utcExpiration,
                senderId: nullableSenderId,
                recipientId: nullableRecipientId,
                subject: request.Subject);
            _dataContext.UserMessageTransfers.Add(transferEntity);
        }

        await _dataContext.SaveChangesAsync();

        string hashId = _hashIdService.Encode(transferId);
        return new UploadTransferResponse(hashId, utcExpiration, transferUserType);
    }
}
