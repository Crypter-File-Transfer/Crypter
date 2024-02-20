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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record SaveFileTransferCommand(
    Maybe<Guid> SenderId,
    Maybe<string> RecipientUsername,
    UploadFileTransferRequest? Request,
    Stream? CiphertextStream)
    : IEitherRequest<UploadTransferError, UploadTransferResponse>;

internal class SaveFileTransferCommandHandler
    : IEitherRequestHandler<SaveFileTransferCommand, UploadTransferError, UploadTransferResponse>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly DataContext _dataContext;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    private readonly IHashIdService _hashIdService;
    private readonly ITransferRepository _transferRepository;
    private readonly TransferStorageSettings _transferStorageSettings;

    public SaveFileTransferCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        DataContext dataContext,
        IHangfireBackgroundService hangfireBackgroundService,
        IHashIdService hashIdService,
        ITransferRepository transferRepository,
        IOptions<TransferStorageSettings> transferStorageSettings)
    {
        _backgroundJobClient = backgroundJobClient;
        _dataContext = dataContext;
        _hangfireBackgroundService = hangfireBackgroundService;
        _hashIdService = hashIdService;
        _transferRepository = transferRepository;
        _transferStorageSettings = transferStorageSettings.Value;
    }
    
    public async Task<Either<UploadTransferError, UploadTransferResponse>> Handle(
        SaveFileTransferCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Request is null || request.CiphertextStream is null)
        {
            return UploadTransferError.UnknownError;
        }

        Task<Either<UploadTransferError, UploadTransferResponse>> responseTask =
            from recipientId in Common.ValidateTransferUploadAsync(
                _dataContext,
                _transferStorageSettings,
                request.SenderId,
                request.RecipientUsername,
                request.Request.LifetimeHours,
                request.CiphertextStream.Length)
            let newTransferId = Guid.NewGuid()
            let transferUserType = Common.DetermineUploadTransferUserType(request.SenderId, recipientId)
            from response in SaveFileTransferAsync(
                newTransferId,
                transferUserType,
                request.SenderId,
                recipientId,
                request.Request,
                request.CiphertextStream)
            from transferNotification in Either<UploadTransferError, Unit>.FromRightAsync(
                Common.QueueTransferNotificationAsync(_backgroundJobClient, _dataContext, _hangfireBackgroundService,
                    newTransferId, TransferItemType.File, recipientId))
            let deletionJobId = Common.ScheduleTransferDeletion(_backgroundJobClient, _hangfireBackgroundService,
                newTransferId, TransferItemType.File, transferUserType, response.ExpirationUTC)
            select response;
        
        return await responseTask;
    }

    private async Task<Either<UploadTransferError, UploadTransferResponse>> SaveFileTransferAsync(
        Guid newTransferId,
        TransferUserType transferUserType,
        Maybe<Guid> senderId,
        Maybe<Guid> recipientId,
        UploadFileTransferRequest request,
        Stream ciphertextStream)
    {
        bool storageSuccess = await _transferRepository.SaveTransferAsync(
            newTransferId,
            TransferItemType.File,
            transferUserType,
            ciphertextStream);
                
        if (!storageSuccess)
        {
            return UploadTransferError.UnknownError;
        }

        return await SaveFileTransferToDatabaseAsync(
            newTransferId,
            senderId,
            recipientId,
            ciphertextStream.Length,
            request,
            transferUserType);
    }
    
    private async Task<UploadTransferResponse> SaveFileTransferToDatabaseAsync(
        Guid transferId,
        Maybe<Guid> senderId,
        Maybe<Guid> recipientId,
        long requiredDiskSpace,
        UploadFileTransferRequest request,
        TransferUserType transferUserType)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime utcExpiration = utcNow.AddHours(request.LifetimeHours);

        Guid? nullableSenderId = senderId.Match<Guid?>(() => null,x  => x);
        Guid? nullableRecipientId = recipientId.Match<Guid?>(() => null,x  => x);

        if (senderId.IsNone && recipientId.IsNone)
        {
            AnonymousFileTransferEntity transferEntity = new AnonymousFileTransferEntity(
                id: transferId,
                size: requiredDiskSpace,
                publicKey: request.PublicKey!,
                keyExchangeNonce: request.KeyExchangeNonce,
                proof: request.Proof,
                created: utcNow,
                expiration: utcExpiration,
                fileName: request.Filename,
                contentType: request.ContentType);
            _dataContext.AnonymousFileTransfers.Add(transferEntity);
        }
        else
        {
            UserFileTransferEntity transferEntity = new UserFileTransferEntity(
                id: transferId,
                size: requiredDiskSpace,
                publicKey: request.PublicKey,
                keyExchangeNonce: request.KeyExchangeNonce,
                proof: request.Proof,
                created: utcNow,
                expiration: utcExpiration,
                senderId: nullableSenderId,
                recipientId: nullableRecipientId,
                fileName: request.Filename,
                contentType: request.ContentType);
            _dataContext.UserFileTransfers.Add(transferEntity);
        }

        await _dataContext.SaveChangesAsync();

        string hashId = _hashIdService.Encode(transferId);
        return new UploadTransferResponse(hashId, utcExpiration, transferUserType);
    }
}
