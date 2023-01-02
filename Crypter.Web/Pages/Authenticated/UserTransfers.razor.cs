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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Common.Enums;
using Crypter.Web.Models;
using Crypter.Web.Pages.Authenticated.Base;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserTransfersBase : AuthenticatedPageBase
   {
      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      protected bool Loading = true;

      protected IEnumerable<UserSentItem> Sent;
      protected IEnumerable<UserReceivedItem> Received;

      protected override async Task OnInitializedAsync()
      {
         await base.OnInitializedAsync();
         if (UserSessionService.Session.IsNone)
         {
            return;
         }

         Loading = false;

         Sent = await GetUserSentItems();
         Received = await GetUserReceivedItems();
      }

      protected async Task<IEnumerable<UserSentItem>> GetUserSentItems()
      {
         var maybeSentMessages = await CrypterApiService.GetSentMessagesAsync();
         var sentMessages = maybeSentMessages.Match(
            new List<UserSentMessageDTO>(),
            right => right.Messages);

         var maybeSentFiles = await CrypterApiService.GetSentFilesAsync();
         var sentFiles = maybeSentFiles.Match(
            new List<UserSentFileDTO>(),
            right => right.Files);

         return sentMessages
            .Select(x => new UserSentItem
            {
               HashId = x.HashId,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               RecipientUsername = x.RecipientUsername,
               RecipientAlias = x.RecipientAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(sentFiles
               .Select(x => new UserSentItem
               {
                  HashId = x.HashId,
                  Name = x.FileName,
                  RecipientUsername = x.RecipientUsername,
                  RecipientAlias = x.RecipientAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }

      protected async Task<IEnumerable<UserReceivedItem>> GetUserReceivedItems()
      {
         var maybeReceivedMessages = await CrypterApiService.GetReceivedMessagesAsync();
         var receivedMessages = maybeReceivedMessages.Match(
            new List<UserReceivedMessageDTO>(),
            right => right.Messages);

         var maybeReceivedFiles = await CrypterApiService.GetReceivedFilesAsync();
         var receivedFiles = maybeReceivedFiles.Match(
            new List<UserReceivedFileDTO>(),
            right => right.Files);

         return receivedMessages
            .Select(x => new UserReceivedItem
            {
               HashId = x.HashId,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               SenderUsername = x.SenderUsername,
               SenderAlias = x.SenderAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(receivedFiles
               .Select(x => new UserReceivedItem
               {
                  HashId = x.HashId,
                  Name = x.FileName,
                  SenderUsername = x.SenderUsername,
                  SenderAlias = x.SenderAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }
   }
}
