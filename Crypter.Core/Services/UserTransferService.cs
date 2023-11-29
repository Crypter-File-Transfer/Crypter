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
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Repositories;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Services;

public interface IUserTransferService
{
    Task<List<UserSentMessageDTO>> GetUserSentMessagesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<UserReceivedMessageDTO>> GetUserReceivedMessagesAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<UserSentFileDTO>> GetUserSentFilesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<UserReceivedFileDTO>> GetUserReceivedFilesAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task DeleteReceivedTransfersAsync(Guid userId);
}

public class UserTransferService : IUserTransferService
{
    private readonly DataContext _context;
    private readonly IHashIdService _hashIdService;
    private readonly ITransferRepository _transferRepository;

    public UserTransferService(DataContext context, IHashIdService hashIdService,
        ITransferRepository transferRepository)
    {
        _context = context;
        _hashIdService = hashIdService;
        _transferRepository = transferRepository;
    }

    public async Task<List<UserSentMessageDTO>> GetUserSentMessagesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var sentMessages = await _context.UserMessageTransfers
            .Where(x => x.SenderId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new { x.Id, x.Subject, x.Recipient.Username, x.Recipient.Profile.Alias, x.Expiration })
            .ToListAsync(cancellationToken);

        List<UserSentMessageDTO> sentMessagesWithHashIds = sentMessages
            .Select(x =>
                new UserSentMessageDTO(_hashIdService.Encode(x.Id), x.Subject, x.Username, x.Alias, x.Expiration))
            .ToList();

        return sentMessagesWithHashIds;
    }

    public async Task<List<UserReceivedMessageDTO>> GetUserReceivedMessagesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var receivedMessages = await _context.UserMessageTransfers
            .Where(x => x.RecipientId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new { x.Id, x.Subject, x.Sender.Username, x.Sender.Profile.Alias, x.Expiration })
            .ToListAsync(cancellationToken);

        List<UserReceivedMessageDTO> receivedMessagesWithHashIds = receivedMessages
            .Select(x =>
                new UserReceivedMessageDTO(_hashIdService.Encode(x.Id), x.Subject, x.Username, x.Alias, x.Expiration))
            .ToList();

        return receivedMessagesWithHashIds;
    }

    public async Task<List<UserSentFileDTO>> GetUserSentFilesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var sentFiles = await _context.UserFileTransfers
            .Where(x => x.SenderId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new { x.Id, x.FileName, x.Recipient.Username, x.Recipient.Profile.Alias, x.Expiration })
            .ToListAsync(cancellationToken);

        List<UserSentFileDTO> sentFilesWithHashIds = sentFiles
            .Select(x =>
                new UserSentFileDTO(_hashIdService.Encode(x.Id), x.FileName, x.Username, x.Alias, x.Expiration))
            .ToList();

        return sentFilesWithHashIds;
    }

    public async Task<List<UserReceivedFileDTO>> GetUserReceivedFilesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var receivedFiles = await _context.UserFileTransfers
            .Where(x => x.RecipientId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new { x.Id, x.FileName, x.Sender.Username, x.Sender.Profile.Alias, x.Expiration })
            .ToListAsync(cancellationToken);

        List<UserReceivedFileDTO> receivedFilesWithHashIds = receivedFiles
            .Select(x =>
                new UserReceivedFileDTO(_hashIdService.Encode(x.Id), x.FileName, x.Username, x.Alias, x.Expiration))
            .ToList();

        return receivedFilesWithHashIds;
    }

    public async Task DeleteReceivedTransfersAsync(Guid userId)
    {
        List<UserFileTransferEntity> receivedFileTransfers = await _context.UserFileTransfers
            .Where(x => x.RecipientId == userId)
            .ToListAsync();

        List<UserMessageTransferEntity> receivedMessageTransfers = await _context.UserMessageTransfers
            .Where(x => x.RecipientId == userId)
            .ToListAsync();

        foreach (var receivedTransfer in receivedFileTransfers)
        {
            _context.Remove(receivedTransfer);
            _transferRepository.DeleteTransfer(receivedTransfer.Id, TransferItemType.File, TransferUserType.User);
        }

        foreach (var receivedTransfer in receivedMessageTransfers)
        {
            _context.Remove(receivedTransfer);
            _transferRepository.DeleteTransfer(receivedTransfer.Id, TransferItemType.Message, TransferUserType.User);
        }

        await _context.SaveChangesAsync();
    }
}
