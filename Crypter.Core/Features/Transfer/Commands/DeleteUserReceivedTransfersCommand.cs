/*
 * Copyright (C) 2023 Crypter File Transfer
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Enums;
using Crypter.Core.Repositories;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

/// <summary>
/// Delete every transfer received by the provided user.
/// </summary>
/// <param name="UserId"></param>
public sealed record DeleteUserReceivedTransfersCommand(Guid UserId) : IRequest<Unit>;

internal sealed class DeleteUserReceivedTransfersCommandHandler
    : IRequestHandler<DeleteUserReceivedTransfersCommand, Unit>
{
    private readonly DataContext _dataContext;
    private readonly ITransferRepository _transferRepository;

    public DeleteUserReceivedTransfersCommandHandler(DataContext dataContext, ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _transferRepository = transferRepository;
    }

    public async Task<Unit> Handle(DeleteUserReceivedTransfersCommand request, CancellationToken cancellationToken)
    {
        List<UserFileTransferEntity> receivedFileTransfers = await _dataContext.UserFileTransfers
            .Where(x => x.RecipientId == request.UserId)
            .ToListAsync(CancellationToken.None);

        List<UserMessageTransferEntity> receivedMessageTransfers = await _dataContext.UserMessageTransfers
            .Where(x => x.RecipientId == request.UserId)
            .ToListAsync(CancellationToken.None);

        _dataContext.UserFileTransfers.RemoveRange(receivedFileTransfers);
        _dataContext.UserMessageTransfers.RemoveRange(receivedMessageTransfers);
        
        foreach (UserFileTransferEntity receivedTransfer in receivedFileTransfers)
        {
            _transferRepository.DeleteTransfer(receivedTransfer.Id, TransferItemType.File, TransferUserType.User);
        }

        foreach (UserMessageTransferEntity receivedTransfer in receivedMessageTransfers)
        {
            _transferRepository.DeleteTransfer(receivedTransfer.Id, TransferItemType.Message, TransferUserType.User);
        }

        await _dataContext.SaveChangesAsync(CancellationToken.None);
        return Unit.Default;
    }
}
