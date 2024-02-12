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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Web.Models;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages.Authenticated;

public partial class UserTransfers
{
    [Inject] private ICrypterApiClient CrypterApiService { get; init; } = null!;

    private bool _loading = true;

    protected IEnumerable<UserSentItem> Sent = [];
    protected IEnumerable<UserReceivedItem> Received = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (UserSessionService.Session.IsNone)
        {
            return;
        }

        _loading = false;

        Sent = await GetUserSentItems();
        Received = await GetUserReceivedItems();
    }

    private async Task<IEnumerable<UserSentItem>> GetUserSentItems()
    {
        Maybe<List<UserSentMessageDTO>> maybeSentMessages = await CrypterApiService.MessageTransfer.GetSentMessagesAsync();
        List<UserSentMessageDTO> sentMessages = maybeSentMessages.SomeOrDefault([]);

        Maybe<List<UserSentFileDTO>> maybeSentFiles = await CrypterApiService.FileTransfer.GetSentFilesAsync();
        List<UserSentFileDTO> sentFiles = maybeSentFiles.SomeOrDefault([]);

        return sentMessages
            .Select(x => new UserSentItem
            {
                HashId = x.HashId,
                Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
                RecipientUsername = x.RecipientUsername,
                RecipientAlias = x.RecipientAlias,
                ItemType = TransferItemType.Message,
                ExpirationUtc = x.ExpirationUTC
            })
            .Concat(sentFiles
                .Select(x => new UserSentItem
                {
                    HashId = x.HashId,
                    Name = x.FileName,
                    RecipientUsername = x.RecipientUsername,
                    RecipientAlias = x.RecipientAlias,
                    ItemType = TransferItemType.File,
                    ExpirationUtc = x.ExpirationUTC
                }))
            .OrderBy(x => x.ExpirationUtc);
    }

    private async Task<IEnumerable<UserReceivedItem>> GetUserReceivedItems()
    {
        Maybe<List<UserReceivedMessageDTO>> maybeReceivedMessages = await CrypterApiService.MessageTransfer.GetReceivedMessagesAsync();
        List<UserReceivedMessageDTO> receivedMessages = maybeReceivedMessages.SomeOrDefault([]);

        Maybe<List<UserReceivedFileDTO>> maybeReceivedFiles = await CrypterApiService.FileTransfer.GetReceivedFilesAsync();
        List<UserReceivedFileDTO> receivedFiles = maybeReceivedFiles.SomeOrDefault([]);

        return receivedMessages
            .Select(x => new UserReceivedItem
            {
                HashId = x.HashId,
                Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
                SenderUsername = x.SenderUsername,
                SenderAlias = x.SenderAlias,
                ItemType = TransferItemType.Message,
                ExpirationUtc = x.ExpirationUTC
            })
            .Concat(receivedFiles
                .Select(x => new UserReceivedItem
                {
                    HashId = x.HashId,
                    Name = x.FileName,
                    SenderUsername = x.SenderUsername,
                    SenderAlias = x.SenderAlias,
                    ItemType = TransferItemType.File,
                    ExpirationUtc = x.ExpirationUTC
                }))
            .OrderBy(x => x.ExpirationUtc);
    }
}
