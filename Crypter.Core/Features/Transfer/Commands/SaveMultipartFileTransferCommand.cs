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
using Crypter.Core.Settings;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record SaveMultipartFileTransferCommand(
    Guid SenderId,
    string HashId,
    int Position,
    Stream? CiphertextStream)
    : IEitherRequest<UploadMultipartFileTransferError, Unit>;

internal class SaveMultipartFileTransferCommandHandler
    : IEitherRequestHandler<SaveMultipartFileTransferCommand, UploadMultipartFileTransferError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;
    private readonly TransferStorageSettings _transferStorageSettings;
    private readonly ILogger<SaveMultipartFileTransferCommandHandler> _logger;

    public SaveMultipartFileTransferCommandHandler(
        DataContext dataContext,
        IHashIdService hashIdService,
        IPublisher publisher,
        ITransferRepository transferRepository,
        IOptions<TransferStorageSettings> transferStorageSettings,
        ILogger<SaveMultipartFileTransferCommandHandler> logger)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferRepository = transferRepository;
        _transferStorageSettings = transferStorageSettings.Value;

        _logger = logger;
    }

    public async Task<Either<UploadMultipartFileTransferError, Unit>> Handle(SaveMultipartFileTransferCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        IExecutionStrategy executionStrategy = _dataContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            Task<Either<UploadMultipartFileTransferError, Unit>> responseTask =
                from additionalData in ValidateRequestAsync(request)
                from saveResult in SavePartAsync(request, additionalData).ToLeftEitherAsync(Unit.Default)
                let successfulMultipartUploadEvent = new SuccessfulMultipartFileTransferUploadEvent(
                    additionalData.InitializedTransferEntity.Id,
                    utcNow)
                from sideEffects in Either<UploadMultipartFileTransferError, Unit>.FromRightAsync(
                    UnitPublisher.Publish(_publisher, successfulMultipartUploadEvent))
                select saveResult;

            return await responseTask
                .DoLeftOrNeitherAsync(
                    async error =>
                    {
                        FailedMultipartFileTransferUploadEvent failedMultipartUploadEvent =
                            new FailedMultipartFileTransferUploadEvent(request.HashId, request.SenderId, error, utcNow);
                        await _publisher.Publish(failedMultipartUploadEvent, CancellationToken.None);
                    },
                    async () =>
                    {
                        FailedMultipartFileTransferUploadEvent failedMultipartUploadEvent =
                            new FailedMultipartFileTransferUploadEvent(request.HashId, request.SenderId, UploadMultipartFileTransferError.UnknownError, utcNow);
                        await _publisher.Publish(failedMultipartUploadEvent, CancellationToken.None);
                    });
        });
    }

    private async Task<Either<UploadMultipartFileTransferError, ValidRequestData>> ValidateRequestAsync(SaveMultipartFileTransferCommand request)
    {
        Guid? itemId = _hashIdService.Decode(request.HashId)
            .Match((Guid?)null, x => x);

        if (!itemId.HasValue)
        {
            return UploadMultipartFileTransferError.NotFound;
        }
        
        if (request.CiphertextStream is null)
        {
            return UploadMultipartFileTransferError.UnknownError;
        }
        
        UserFileTransferEntity? initializedTransferEntity = await _dataContext.UserFileTransfers
            .Where(x => x.Id == itemId.Value
                        && x.SenderId == request.SenderId
                        && x.Parts)
            .FirstOrDefaultAsync(CancellationToken.None);
        if (initializedTransferEntity is null)
        {
            return UploadMultipartFileTransferError.NotFound;
        }
        
        long maximumTransferSize = Convert.ToInt64(_transferStorageSettings.MaximumTransferSizeMB * Math.Pow(10, 6));
        long updatedTransferSize = _transferRepository.GetTransferPartsSize(itemId.Value, TransferItemType.File, TransferUserType.User)
            + request.CiphertextStream.Length;
        
        _logger.LogError(updatedTransferSize.ToString());

        if (updatedTransferSize > maximumTransferSize)
        {
            return UploadMultipartFileTransferError.AggregateTooLarge;
        }
        
        return new ValidRequestData(initializedTransferEntity, updatedTransferSize);
    }

    private async Task<Maybe<UploadMultipartFileTransferError>> SavePartAsync(SaveMultipartFileTransferCommand request, ValidRequestData additionalData)
    {
        bool saveSuccess = await _transferRepository.SaveTransferPartAsync(
            additionalData.InitializedTransferEntity.Id,
            TransferItemType.File,
            TransferUserType.User,
            request.CiphertextStream!,
            request.Position);

        if (!saveSuccess)
        {
            return UploadMultipartFileTransferError.UnknownError;
        }
        
        additionalData.InitializedTransferEntity.Size = additionalData.UpdatedTransferSize;
        await _dataContext.SaveChangesAsync();
        return default;
    }

    private class ValidRequestData
    {
        public UserFileTransferEntity InitializedTransferEntity { get; init; }
        public long UpdatedTransferSize { get; init; }

        public ValidRequestData(UserFileTransferEntity initializedTransferEntity, long updatedTransferSize)
        {
            InitializedTransferEntity = initializedTransferEntity;
            UpdatedTransferSize = updatedTransferSize;
        }
    }
}
