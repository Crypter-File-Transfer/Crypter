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

public sealed record GetUserFileCiphertextCommand(string HashId, byte[] Proof, Maybe<Guid> RequestorId)
    : IEitherRequest<DownloadTransferCiphertextError, FileStream>;

internal class GetUserFileCiphertextCommandHandler
    : IEitherRequestHandler<GetUserFileCiphertextCommand, DownloadTransferCiphertextError, FileStream>
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly IPublisher _publisher;
    private readonly ITransferRepository _transferRepository;
    
    public GetUserFileCiphertextCommandHandler(
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

    public async Task<Either<DownloadTransferCiphertextError, FileStream>> Handle(GetUserFileCiphertextCommand request, CancellationToken cancellationToken)
    {
        Guid? nullableRequesterUserId = request.RequestorId
            .Match<Guid?>(() => null, x => x);

        Guid itemId = _hashIdService.Decode(request.HashId);
        var databaseData = await _dataContext.UserFileTransfers
            .Where(x => x.Id == itemId)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequesterUserId)
            .Select(x => new { x.RecipientId, x.Proof })
            .FirstOrDefaultAsync(CancellationToken.None);

        bool ciphertextExists = _transferRepository.TransferExists(itemId, TransferItemType.File, TransferUserType.User);
        if (databaseData is null || !ciphertextExists)
        {
            return DownloadTransferCiphertextError.NotFound;
        }

        if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, request.Proof))
        {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
        }

        bool deleteFromTransferRepoUponReadCompletion = !databaseData.RecipientId.HasValue;
        return await _transferRepository
            .GetTransfer(itemId, TransferItemType.File, TransferUserType.User, deleteFromTransferRepoUponReadCompletion)
            .IfSomeAsync(async _ =>
            {
                SuccessfulTransferDownloadEvent downloadEvent = new SuccessfulTransferDownloadEvent(itemId, TransferItemType.File, TransferUserType.User, nullableRequesterUserId, !deleteFromTransferRepoUponReadCompletion, DateTimeOffset.UtcNow);
                await _publisher.Publish(downloadEvent, CancellationToken.None);
            })
            .ToEitherAsync(DownloadTransferCiphertextError.NotFound);
    }
}
