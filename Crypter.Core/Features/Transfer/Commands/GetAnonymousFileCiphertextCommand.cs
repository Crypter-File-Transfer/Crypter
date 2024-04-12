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
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer.Commands;

public sealed record GetAnonymousFileCiphertextCommand(string HashId, byte[] Proof)
    : IEitherRequest<DownloadTransferCiphertextError, FileStream>;

internal class GetAnonymousFileCiphertextCommandHandler
    : IEitherRequestHandler<GetAnonymousFileCiphertextCommand, DownloadTransferCiphertextError, FileStream>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    private readonly IHashIdService _hashIdService;
    private readonly ITransferRepository _transferRepository;
    
    public GetAnonymousFileCiphertextCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IHangfireBackgroundService hangfireBackgroundService,
        IHashIdService hashIdService,
        ITransferRepository transferRepository)
    {
        _backgroundJobClient = backgroundJobClient;
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _hangfireBackgroundService = hangfireBackgroundService;
        _hashIdService = hashIdService;
        _transferRepository = transferRepository;
    }
    
    public async Task<Either<DownloadTransferCiphertextError, FileStream>> Handle(GetAnonymousFileCiphertextCommand request, CancellationToken cancellationToken)
    {
        Guid id = _hashIdService.Decode(request.HashId);
        var databaseData = await _dataContext.AnonymousFileTransfers
            .Where(x => x.Id == id)
            .Select(x => new { x.Proof })
            .FirstOrDefaultAsync(CancellationToken.None);

        bool ciphertextExists =
            _transferRepository.TransferExists(id, TransferItemType.File, TransferUserType.Anonymous);
        if (databaseData is null || !ciphertextExists)
        {
            return DownloadTransferCiphertextError.NotFound;
        }

        if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, request.Proof))
        {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
        }

        const bool deleteOnReadCompletion = true;
        return _transferRepository
            .GetTransfer(id, TransferItemType.File, TransferUserType.Anonymous, deleteOnReadCompletion)
            .IfSome(_ => _backgroundJobClient
                .Enqueue(() => _hangfireBackgroundService
                    .DeleteTransferAsync(id, TransferItemType.File, TransferUserType.Anonymous, !deleteOnReadCompletion)))
            .ToEither(DownloadTransferCiphertextError.NotFound);
    }
}
