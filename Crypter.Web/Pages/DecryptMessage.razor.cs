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

using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class DecryptMessageBase : ComponentBase
   {
      [Inject]
      ILocalStorageService LocalStorage { get; set; }

      [Inject]
      protected ITransferApiService TransferService { get; set; }

      [Parameter]
      public Guid TransferId { get; set; }

      protected bool Loading;
      protected bool ItemFound;

      protected string Subject;
      protected int Size;
      protected string Created;
      protected string Expiration;
      
      protected Guid SenderId;
      protected string SenderUsername;
      protected string SenderAlias;
      protected string X25519PublicKey;

      protected Guid RecipientId;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         await PrepareMessagePreviewAsync();
         await base.OnInitializedAsync();
         Loading = false;
      }

      protected async Task PrepareMessagePreviewAsync()
      {
         var messagePreviewRequest = new DownloadTransferPreviewRequest(TransferId);
         var withAuth = LocalStorage.HasItem(StoredObjectType.UserSession);
         ItemFound = (await TransferService.DownloadMessagePreviewAsync(messagePreviewRequest, withAuth))
            .Match(
               left => false,
               right =>
               {
                  Subject = string.IsNullOrEmpty(right.Subject)
                     ? "{ no subject }"
                     : right.Subject;
                  Created = right.CreationUTC.ToLocalTime().ToString();
                  Expiration = right.ExpirationUTC.ToLocalTime().ToString();
                  Size = right.Size;
                  SenderId = right.SenderId;
                  SenderUsername = right.SenderUsername;
                  SenderAlias = right.SenderAlias;
                  RecipientId = right.RecipientId;
                  X25519PublicKey = right.X25519PublicKey;
                  return true;
               }
            );
      }
   }
}
