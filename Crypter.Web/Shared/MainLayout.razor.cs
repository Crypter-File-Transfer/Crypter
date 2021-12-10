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
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class MainLayoutBase : LayoutComponentBase
   {
      [Inject]
      private NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ILocalStorageService LocalStorage { get; set; }

      protected Modal.ReAuthenticationModal ReAuthenticationModal { get; set; }

      protected override async Task OnInitializedAsync()
      {
         await LocalStorage.InitializeAsync();
         StateHasChanged();

         if (UserNeedsReauthentication())
         {
            ReAuthenticationModal.Open();
            return;
         }

         if (LocalStorage.HasItem(StoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/user/home");
         }
      }

      protected bool UserNeedsReauthentication()
      {
         // User session does not exist. There is nobody to reauthenticate.
         if (!LocalStorage.HasItem(StoredObjectType.UserSession))
         {
            return false;
         }

         // User session and the auth token both exist. No need to reauthenticate.
         if (LocalStorage.HasItem(StoredObjectType.UserSession)
            && LocalStorage.HasItem(StoredObjectType.AuthToken))
         {
            return false;
         }

         // Reauthenticate!
         return true;
      }

      public void OnReauthenticationModalClosed()
      {
         StateHasChanged();
      }
   }
}
