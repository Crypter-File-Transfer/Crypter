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
using System.Data;
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
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record AbandonMultipartFileTransferCommand(Guid SenderId, string HashId)
    : IEitherRequest<AbandonMultipartFileTransferError, Unit>;

internal class AbandonMultipartFileTransferCommandHandler
    : IEitherRequestHandler<AbandonMultipartFileTransferCommand, AbandonMultipartFileTransferError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;
    
    public AbandonMultipartFileTransferCommandHandler(
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

    public async Task<Either<AbandonMultipartFileTransferError, Unit>> Handle(AbandonMultipartFileTransferCommand request, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await _dataContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, CancellationToken.None);

        try
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            Task<Either<AbandonMultipartFileTransferError, Unit>> responseTask =
                from additionalData in ValidateRequestAsync(request)
                from abandonResult in Either<AbandonMultipartFileTransferError, Unit>.FromRightAsync(
                    AbandonAsync(additionalData))
                let successfulMultipartAbandonEvent = new SuccessfulMultipartFileTransferAbandonEvent(
                    additionalData.InitializedTransferEntity.Id,
                    TransferItemType.File,
                    utcNow)
                from sideEffects in Either<AbandonMultipartFileTransferError, Unit>.FromRightAsync(
                    UnitPublisher.Publish(_publisher, successfulMultipartAbandonEvent))
                select abandonResult;

            return await responseTask
                .DoLeftOrNeitherAsync(
                    async error =>
                    {
                        FailedMultipartFileTransferAbandonEvent failedMultipartAbandonEvent =
                            new FailedMultipartFileTransferAbandonEvent(request.HashId, request.SenderId, error,
                                utcNow);
                        await _publisher.Publish(failedMultipartAbandonEvent, CancellationToken.None);
                    },
                    async () =>
                    {
                        FailedMultipartFileTransferAbandonEvent failedMultipartAbandonEvent =
                            new FailedMultipartFileTransferAbandonEvent(request.HashId, request.SenderId, AbandonMultipartFileTransferError.UnknownError, utcNow);
                        await _publisher.Publish(failedMultipartAbandonEvent, CancellationToken.None);
                    });
        }
        finally
        {
            await transaction.CommitAsync(CancellationToken.None);
        }
    }

    private async Task<Either<AbandonMultipartFileTransferError, ValidRequestData>> ValidateRequestAsync(AbandonMultipartFileTransferCommand request)
    {
        Guid? itemId = _hashIdService.Decode(request.HashId)
            .Match((Guid?)null, x => x);

        if (!itemId.HasValue)
        {
            return AbandonMultipartFileTransferError.NotFound;
        }
        
        UserFileTransferEntity? initializedTransferEntity = await _dataContext.UserFileTransfers
            .Where(x => x.Id == itemId.Value
                        && x.SenderId == request.SenderId
                        && x.Parts)
            .FirstOrDefaultAsync(CancellationToken.None);
        if (initializedTransferEntity is null)
        {
            return AbandonMultipartFileTransferError.NotFound;
        }

        return new ValidRequestData(initializedTransferEntity);
    }

    private async Task<Unit> AbandonAsync(ValidRequestData additionalData)
    {
        _transferRepository.DeleteTransferParts(additionalData.InitializedTransferEntity.Id, TransferItemType.File, TransferUserType.User);
        _dataContext.UserFileTransfers.Remove(additionalData.InitializedTransferEntity);
        await _dataContext.SaveChangesAsync();
        return Unit.Default;
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
