/*
 * Copyright (C) 2022 Crypter File Transfer
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
using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class DecryptMessageBase : ComponentBase
   {
      [Inject]
      IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> BrowserStorageService { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

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
         var withAuth = BrowserStorageService.HasItem(BrowserStoredObjectType.UserSession);
         var response = await CrypterApiService.DownloadMessagePreviewAsync(messagePreviewRequest, withAuth);
         response.DoRight(x =>
         {
            Subject = string.IsNullOrEmpty(x.Subject)
               ? "{ no subject }"
               : x.Subject;
            Created = x.CreationUTC.ToLocalTime().ToString();
            Expiration = x.ExpirationUTC.ToLocalTime().ToString();
            Size = x.Size;
            SenderId = x.SenderId;
            SenderUsername = x.SenderUsername;
            SenderAlias = x.SenderAlias;
            RecipientId = x.RecipientId;
            X25519PublicKey = x.X25519PublicKey;
         });

         ItemFound = response.IsRight;
      }
   }
}
