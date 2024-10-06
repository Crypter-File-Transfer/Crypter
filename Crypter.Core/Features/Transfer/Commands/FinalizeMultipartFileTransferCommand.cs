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
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Guid = System.Guid;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record FinalizeMultipartFileTransferCommand(Guid SenderId, string HashId)
    : IEitherRequest<FinalizeMultipartFileTransferError, Unit>;

internal class FinalizeMultipartFileTransferCommandHandler
    : IEitherRequestHandler<FinalizeMultipartFileTransferCommand, FinalizeMultipartFileTransferError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;
    
    public FinalizeMultipartFileTransferCommandHandler(
        DataContext dataContext,
        IHashIdService hashIdService,
        IPublisher publisher,
        ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _publisher = publisher;
        _transferRepository = transferRepository;
    }

    public async Task<Either<FinalizeMultipartFileTransferError, Unit>> Handle(FinalizeMultipartFileTransferCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        IExecutionStrategy executionStrategy = _dataContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            Task<Either<FinalizeMultipartFileTransferError, Unit>> responseTask =
                from additionalData in ValidateRequestAsync(request)
                from finalizeResult in FinalizeAsync(additionalData).ToLeftEitherAsync(Unit.Default)
                let successfulMultipartFinalizeEvent = new SuccessfulMultipartFileTransferFinalizationEvent(
                    additionalData.InitializedTransferEntity.Id,
                    additionalData.InitializedTransferEntity.RecipientId ?? Maybe<Guid>.None,
                    utcNow)
                from sideEffects in Either<FinalizeMultipartFileTransferError, Unit>.FromRightAsync(
                    UnitPublisher.Publish(_publisher, successfulMultipartFinalizeEvent))
                select finalizeResult;

            return await responseTask
                .DoLeftOrNeitherAsync(
                    async error =>
                    {
                        FailedMultipartFileTransferFinalizationEvent failedMultipartFinalizationEvent =
                            new FailedMultipartFileTransferFinalizationEvent(request.HashId, request.SenderId, error,
                                utcNow);
                        await _publisher.Publish(failedMultipartFinalizationEvent, CancellationToken.None);
                    },
                    async () =>
                    {
                        FailedMultipartFileTransferFinalizationEvent failedMultipartFinalizationEvent =
                            new FailedMultipartFileTransferFinalizationEvent(request.HashId, request.SenderId,
                                FinalizeMultipartFileTransferError.UnknownError, utcNow);
                        await _publisher.Publish(failedMultipartFinalizationEvent, CancellationToken.None);
                    });
        });
    }

    private async Task<Either<FinalizeMultipartFileTransferError, ValidRequestData>> ValidateRequestAsync(FinalizeMultipartFileTransferCommand request)
    {
        Guid? itemId = _hashIdService.Decode(request.HashId)
            .Match((Guid?)null, x => x);

        if (!itemId.HasValue)
        {
            return FinalizeMultipartFileTransferError.NotFound;
        }
        
        UserFileTransferEntity? initializedTransferEntity = await _dataContext.UserFileTransfers
            .Where(x => x.Id == itemId.Value
                        && x.SenderId == request.SenderId
                        && x.Parts)
            .FirstOrDefaultAsync(CancellationToken.None);
        if (initializedTransferEntity is null)
        {
            return FinalizeMultipartFileTransferError.NotFound;
        }

        return new ValidRequestData(initializedTransferEntity);
    }

    private async Task<Maybe<FinalizeMultipartFileTransferError>> FinalizeAsync(ValidRequestData additionalData)
    {
        bool finalizeSuccess = await _transferRepository.JoinTransferPartsAsync(
            additionalData.InitializedTransferEntity.Id,
            TransferItemType.File,
            TransferUserType.User);

        if (!finalizeSuccess)
        {
            return FinalizeMultipartFileTransferError.UnknownError;
        }

        additionalData.InitializedTransferEntity.Parts = false;
        additionalData.InitializedTransferEntity.Size = _transferRepository.GetTransferSize(
            additionalData.InitializedTransferEntity.Id,
            TransferItemType.File,
            TransferUserType.User);
        await _dataContext.SaveChangesAsync();
        return default;
    }
    
    private sealed class ValidRequestData
    {
        public UserFileTransferEntity InitializedTransferEntity { get; init; }
        
        public ValidRequestData(UserFileTransferEntity initializedTransferEntity)
        {
            InitializedTransferEntity = initializedTransferEntity;
        }
    }
}
