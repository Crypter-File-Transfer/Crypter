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
using Crypter.Common.Enums;
using Crypter.Core.Repositories;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Commands;

/// <summary>
/// Delete the provided transfer from the database.
/// </summary>
/// <param name="ItemId"></param>
/// <param name="ItemType"></param>
/// <param name="UserType"></param>
/// <param name="DeleteFromTransferStorage">'True' to also delete the transfer from transfer storage.</param>
public sealed record DeleteTransferCommand(Guid ItemId, TransferItemType ItemType, TransferUserType UserType,
    bool DeleteFromTransferStorage) : IRequest<Unit>;

internal sealed class DeleteTransferCommandHandler : IRequestHandler<DeleteTransferCommand, Unit>
{
    private readonly DataContext _dataContext;
    private readonly ITransferRepository _transferRepository;

    public DeleteTransferCommandHandler(DataContext dataContext, ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _transferRepository = transferRepository;
    }
    
    public async Task<Unit> Handle(DeleteTransferCommand request, CancellationToken cancellationToken)
    {
        bool entityFound = false;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (request.ItemType)
        {
            case TransferItemType.Message:
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (request.UserType)
                {
                    case TransferUserType.Anonymous:
                        AnonymousMessageTransferEntity? anonymousEntity = await _dataContext.AnonymousMessageTransfers
                                .FirstOrDefaultAsync(x => x.Id == request.ItemId, CancellationToken.None);
                        if (anonymousEntity is not null)
                        {
                            _dataContext.AnonymousMessageTransfers.Remove(anonymousEntity);
                            entityFound = true;
                        }
                        break;
                    
                    case TransferUserType.User:
                        UserMessageTransferEntity? userEntity = await _dataContext.UserMessageTransfers
                                .FirstOrDefaultAsync(x => x.Id == request.ItemId, CancellationToken.None);
                        if (userEntity is not null)
                        {
                            _dataContext.UserMessageTransfers.Remove(userEntity);
                            entityFound = true;
                        }
                        break;
                }
                break;
            
            case TransferItemType.File:
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (request.UserType)
                {
                    case TransferUserType.Anonymous:
                        AnonymousFileTransferEntity? anonymousEntity = await _dataContext.AnonymousFileTransfers
                                .FirstOrDefaultAsync(x => x.Id == request.ItemId, CancellationToken.None);
                        if (anonymousEntity is not null)
                        {
                            _dataContext.AnonymousFileTransfers.Remove(anonymousEntity);
                            entityFound = true;
                        }
                        break;
                    
                    case TransferUserType.User:
                        UserFileTransferEntity? userEntity = await _dataContext.UserFileTransfers
                            .FirstOrDefaultAsync(x => x.Id == request.ItemId, CancellationToken.None);
                        if (userEntity is not null)
                        {
                            _dataContext.UserFileTransfers.Remove(userEntity);
                            entityFound = true;
                        }
                        break;
                }
                break;
        }

        if (entityFound)
        {
            await _dataContext.SaveChangesAsync(CancellationToken.None);
        }

        if (request.DeleteFromTransferStorage)
        {
            _transferRepository.DeleteTransfer(request.ItemId, request.ItemType, request.UserType);
        }

        return Unit.Default;
    }
}
