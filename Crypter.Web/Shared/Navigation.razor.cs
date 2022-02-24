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
using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class NavigationBase : ComponentBase, IDisposable
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> BrowserStorageService { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      protected UploadFileTransferModal FileTransferModal { get; set; }

      protected UploadMessageTransferModal MessageTransferModal { get; set; }

      protected bool ShowUserNavigation { get; set; } = false;

      protected string Username { get; set; }

      protected string ProfileUrl { get; set; }

      protected string SearchKeyword { get; set; }

      protected override async Task OnInitializedAsync()
      {
         var session = await BrowserStorageService.GetItemAsync<UserSession>(BrowserStoredObjectType.UserSession);
         ShowUserNavigation = session is not null;
         if (session is not null)
         {
            Username = session.Username;
            ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
         }
         NavigationManager.LocationChanged += HandleLocationChanged;
         AuthenticationService.UserSessionStateChanged += HandleUserSessionStateChanged;
      }

      protected async Task OnLogoutClicked()
      {
         await AuthenticationService.LogoutAsync();
         NavigationManager.NavigateTo("/");
      }

      protected void HandleLocationChanged(object sender, LocationChangedEventArgs e)
      {
         InvokeAsync(async () =>
         {
            await CollapseNavigationMenuAsync();
         });
      }

      protected void HandleUserSessionStateChanged(object sender, UserSessionStateChangedEventArgs e)
      {
         ShowUserNavigation = e.LoggedIn;
         if (e.LoggedIn)
         {
            Username = e.Username;
            ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
         }
         StateHasChanged();
      }

      protected async Task OnEncryptFileClicked()
      {
         await FileTransferModal.Open();
      }

      protected async Task OnEncryptMessageClicked()
      {
         await MessageTransferModal.Open();
      }

      protected async Task CollapseNavigationMenuAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CollapseNavBar");
      }

      protected void OnSearchClicked()
      {
         NavigationManager.NavigateTo($"/user/search?query={SearchKeyword}&type=username&page=1");
      }

      public void Dispose()
      {
         NavigationManager.LocationChanged -= HandleLocationChanged;
         AuthenticationService.UserSessionStateChanged -= HandleUserSessionStateChanged;
         GC.SuppressFinalize(this);
      }
   }
}
