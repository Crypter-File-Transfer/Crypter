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

using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserProfileBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      IUserApiService UserService { get; set; }

      [Inject]
      ILocalStorageService LocalStorage { get; set; }

      [Parameter]
      public string Username { get; set; }

      protected Shared.Modal.UploadFileTransferModal FileModal { get; set; }
      protected Shared.Modal.UploadMessageTransferModal MessageModal { get; set; }

      protected bool Loading;
      protected bool ProfileFound;
      protected Guid UserId;
      protected string Alias;
      protected string About;
      protected string ActualUsername;
      protected bool AllowsFiles;
      protected bool AllowsMessages;
      protected string UserX25519PublicKey;
      protected string UserEd25519PublicKey;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;

         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - User Profile");
         await base.OnInitializedAsync();

         await PrepareUserProfileAsync();

         Loading = false;
      }

      protected async Task PrepareUserProfileAsync()
      {
         var requestWithAuthentication = LocalStorage.HasItem(StoredObjectType.UserSession);
         var (httpStatus, response) = await UserService.GetUserPublicProfileAsync(Username, requestWithAuthentication);
         ProfileFound = httpStatus != HttpStatusCode.NotFound;
         if (ProfileFound)
         {
            UserId = response.Id;
            Alias = response.Alias;
            About = response.About;
            ActualUsername = response.Username;
            AllowsFiles = response.ReceivesFiles;
            AllowsMessages = response.ReceivesMessages;
            UserX25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(response.PublicDHKey));
            UserEd25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(response.PublicDSAKey));
         }
      }
   }
}
