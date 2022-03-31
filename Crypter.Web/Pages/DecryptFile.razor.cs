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
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class DecryptFileBase : ComponentBase
   {
      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Parameter]
      public Guid TransferId { get; set; }

      protected bool Loading;
      protected bool ItemFound;

      protected string FileName;
      protected string ContentType;
      protected int Size;
      protected string Created;
      protected string Expiration;

      protected string SenderUsername;
      protected string SenderAlias;
      protected string X25519PublicKey;

      protected string RecipientUsername;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         await PrepareFilePreviewAsync();
         Loading = false;
      }

      protected async Task PrepareFilePreviewAsync()
      {
         var filePreviewRequest = new DownloadTransferPreviewRequest(TransferId);
         var response = await CrypterApiService.DownloadFilePreviewAsync(filePreviewRequest, UserSessionService.LoggedIn);
         response.DoRight(x =>
         {
            FileName = x.FileName;
            ContentType = x.ContentType;
            Created = x.CreationUTC.ToLocalTime().ToString();
            Expiration = x.ExpirationUTC.ToLocalTime().ToString();
            Size = x.Size;
            SenderUsername = x.Sender;
            SenderAlias = x.SenderAlias;
            RecipientUsername = x.Recipient;
            X25519PublicKey = x.X25519PublicKey;
         });

         ItemFound = response.IsRight;
      }
   }
}
