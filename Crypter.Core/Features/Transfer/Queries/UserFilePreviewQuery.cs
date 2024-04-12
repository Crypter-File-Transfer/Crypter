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
using Crypter.Core.MediatorMonads;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer.Queries;

public sealed record UserFilePreviewQuery(string HashId, Maybe<Guid> RequestorId)
    : IEitherRequest<TransferPreviewError, FileTransferPreviewResponse>;

internal class UserFilePreviewQueryHandler
    : IEitherRequestHandler<UserFilePreviewQuery, TransferPreviewError, FileTransferPreviewResponse>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    private readonly ITransferRepository _transferRepository;

    public UserFilePreviewQueryHandler(DataContext dataContext, IHashIdService hashIdService, ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
        _transferRepository = transferRepository;
    }
    
    public async Task<Either<TransferPreviewError, FileTransferPreviewResponse>> Handle(UserFilePreviewQuery request, CancellationToken cancellationToken)
    {
        Guid? nullableRequestorUserId = request.RequestorId
            .Match<Guid?>(() => null, x => x);

        Guid id = _hashIdService.Decode(request.HashId);

        FileTransferPreviewResponse? filePreview = await _dataContext.UserFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequestorUserId)
            .Select(x => new FileTransferPreviewResponse(
                x.FileName,
                x.ContentType,
                x.Size,
                x.Sender!.Username,
                x.Sender!.Profile!.Alias,
                x.Recipient!.Username,
                x.SenderId == null
                    ? x.PublicKey!
                    : x.Sender!.KeyPair!.PublicKey,
                x.KeyExchangeNonce,
                x.Created,
                x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

        if (filePreview is null)
        {
            return TransferPreviewError.NotFound;
        }
        
        bool ciphertextExists =
            _transferRepository.TransferExists(id, TransferItemType.File, TransferUserType.User);

        if (!ciphertextExists)
        {
            return TransferPreviewError.NotFound;
        }
        
        return filePreview;
    }
}
