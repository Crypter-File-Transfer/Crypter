/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Contracts.Enum;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserHomeBase : ComponentBase
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      ILocalStorageService LocalStorage { get; set; }

      [Inject]
      IUserApiService UserService { get; set; }

      protected IEnumerable<UserSentItem> Sent;
      protected IEnumerable<UserReceivedItem> Received;

      protected override async Task OnInitializedAsync()
      {
         if (!LocalStorage.HasItem(StoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         await base.OnInitializedAsync();

         Sent = await GetUserSentItems();
         Received = await GetUserReceivedItems();
      }

      protected async Task<IEnumerable<UserSentItem>> GetUserSentItems()
      {
         var (messageRequestStatus, sentMessagesResponse) = await UserService.GetUserSentMessagesAsync();
         var (fileRequestStatus, sentFilesresponse) = await UserService.GetUserSentFilesAsync();

         if (messageRequestStatus != HttpStatusCode.OK
            || fileRequestStatus != HttpStatusCode.OK)
         {
            return default;
         }

         return sentMessagesResponse.Messages
            .Select(x => new UserSentItem
            {
               Id = x.Id,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               RecipientId = x.RecipientId,
               RecipientUsername = x.RecipientUsername,
               RecipientAlias = x.RecipientAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(sentFilesresponse.Files
               .Select(x => new UserSentItem
               {
                  Id = x.Id,
                  Name = x.FileName,
                  RecipientId = x.RecipientId,
                  RecipientUsername = x.RecipientUsername,
                  RecipientAlias = x.RecipientAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }

      protected async Task<IEnumerable<UserReceivedItem>> GetUserReceivedItems()
      {
         var (messageRequestStatus, receivedMessagesResponse) = await UserService.GetUserReceivedMessagesAsync();
         var (fileRequestStatus, receivedFilesresponse) = await UserService.GetUserReceivedFilesAsync();

         if (messageRequestStatus != HttpStatusCode.OK
            || fileRequestStatus != HttpStatusCode.OK)
         {
            return default;
         }

         return receivedMessagesResponse.Messages
            .Select(x => new UserReceivedItem
            {
               Id = x.Id,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               SenderId = x.SenderId,
               SenderUsername = x.SenderUsername,
               SenderAlias = x.SenderAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(receivedFilesresponse.Files
               .Select(x => new UserReceivedItem
               {
                  Id = x.Id,
                  Name = x.FileName,
                  SenderId = x.SenderId,
                  SenderUsername = x.SenderUsername,
                  SenderAlias = x.SenderAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }
   }
}
