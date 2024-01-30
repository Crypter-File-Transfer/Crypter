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
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.LinqExpressions;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Notifications.Commands;

/// <summary>
/// A command that will attempt to send a transfer notification email to the
/// recipient of the provided transfer..
///
/// Returns 'true' if the email was successfully sent or if the state of the
/// system is such that the email should not be sent.
/// 
/// Returns 'false' if the email should be sent, but failed to send.
/// </summary>
/// <param name="ItemId"></param>
/// <param name="ItemType"></param>
public sealed record SendTransferNotificationCommand(Guid ItemId, TransferItemType ItemType)
    : IRequest<bool>;

internal sealed class SendTransferNotificationCommandHandler
    : IRequestHandler<SendTransferNotificationCommand, bool>
{
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;

    public SendTransferNotificationCommandHandler(DataContext dataContext,
        IEmailService emailService)
    {
        _dataContext = dataContext;
        _emailService = emailService;
    }

    public async Task<bool> Handle(SendTransferNotificationCommand request, CancellationToken cancellationToken)
    {
        UserEntity? recipient = request.ItemType switch
        {
            TransferItemType.Message => await _dataContext.UserMessageTransfers
                .Where(x => x.Id == request.ItemId)
                .Select(x => x.Recipient)
                .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                .FirstOrDefaultAsync(CancellationToken.None),
            TransferItemType.File => await _dataContext.UserFileTransfers
                .Where(x => x.Id == request.ItemId)
                .Select(x => x.Recipient)
                .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                .FirstOrDefaultAsync(CancellationToken.None),
            _ => null
        };

        if (recipient is not null
            && EmailAddress.TryFrom(recipient.EmailAddress!, out EmailAddress validEmailAddress))
        {
            return await _emailService.SendTransferNotificationAsync(validEmailAddress);
        }

        return true;
    }
}
