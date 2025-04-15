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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Features.Transfer.Events;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record InitializeMultipartFileTransferCommand(
    Guid SenderId,
    Maybe<string> RecipientUsername,
    UploadFileTransferRequest? Request)
    : IEitherRequest<UploadTransferError, InitiateMultipartFileTransferResponse>;

internal class InitializeMultipartFileTransferCommandHandler
    : IEitherRequestHandler<InitializeMultipartFileTransferCommand, UploadTransferError, InitiateMultipartFileTransferResponse>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly TransferStorageSettings _transferStorageSettings;
    
    public InitializeMultipartFileTransferCommandHandler(
        DataContext dataContext,
        IHashIdService hashIdService,
        IPublisher publisher,
        IOptions<TransferStorageSettings> transferStorageSettings)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferStorageSettings = transferStorageSettings.Value;
    }
    
    public async Task<Either<UploadTransferError, InitiateMultipartFileTransferResponse>> Handle(
        InitializeMultipartFileTransferCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset eventTimestamp = DateTimeOffset.UtcNow;
        Task<Either<UploadTransferError, InitiateMultipartFileTransferResponse>> responseTask;
        
        if (request.Request is null)
        {
            responseTask = Either<UploadTransferError, InitiateMultipartFileTransferResponse>
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
                    TransferItemType.File,
                    request.Request.LifetimeHours,
                    null)
                let transferUserType = Common.DetermineUploadTransferUserType(request.SenderId, recipientId)
                let newTransferId = Guid.NewGuid()
                from response in InitializeFileTransferToDatabaseAsync(
                    newTransferId,
                    request.SenderId,
                    recipientId,
                    request.Request,
                    transferUserType)
                let successfulInitializationEvent = new SuccessfulMultipartFileTransferInitializationEvent(
                    newTransferId,
                    request.SenderId,
                    request.RecipientUsername,
                    response.Expiration,
                    eventTimestamp)
                from sideEffects in Either<UploadTransferError, Unit>.FromRightAsync(
                    UnitPublisher.Publish(_publisher, successfulInitializationEvent))
                select response;
        }

        return await responseTask
            .DoLeftOrNeitherAsync(
                async error =>
                {
                    FailedMultipartFileTransferInitializationEvent failedMultipartFileInitializationEvent = new FailedMultipartFileTransferInitializationEvent(error, request.SenderId, request.RecipientUsername, eventTimestamp);
                    await _publisher.Publish(failedMultipartFileInitializationEvent, CancellationToken.None);
                },
                async () =>
                {
                    FailedMultipartFileTransferInitializationEvent failedMultipartFileInitializationEvent = new FailedMultipartFileTransferInitializationEvent(UploadTransferError.UnknownError, request.SenderId, request.RecipientUsername, eventTimestamp);
                    await _publisher.Publish(failedMultipartFileInitializationEvent, CancellationToken.None);
                });
        }
    
    private async Task<Either<UploadTransferError, InitiateMultipartFileTransferResponse>> InitializeFileTransferToDatabaseAsync(
        Guid transferId,
        Guid senderId,
        Maybe<Guid> recipientId,
        UploadFileTransferRequest request,
        TransferUserType transferUserType)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiration = now.AddHours(request.LifetimeHours);
        
        Guid? nullableRecipientId = recipientId.Match<Guid?>(() => null,x  => x);
        
        UserFileTransferEntity transferEntity = new UserFileTransferEntity(
            id: transferId,
            size: 0,
            publicKey: request.PublicKey,
            keyExchangeNonce: request.KeyExchangeNonce,
            proof: request.Proof,
            created: now.UtcDateTime,
            expiration: expiration.UtcDateTime,
            senderId: senderId,
            recipientId: nullableRecipientId,
            fileName: request.Filename,
            contentType: request.ContentType,
            parts: true);
        _dataContext.UserFileTransfers.Add(transferEntity);

        await _dataContext.SaveChangesAsync();

        string hashId = _hashIdService.Encode(transferId);
        return new InitiateMultipartFileTransferResponse(hashId, transferUserType, expiration);
    }
}
