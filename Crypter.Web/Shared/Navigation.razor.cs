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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Repositories;
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
      protected IJSRuntime JSRuntime { get; set; }

      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      [Inject]
      protected IDeviceRepository<BrowserStorageLocation> BrowserRepository { get; set; }

      protected UploadFileTransferModal FileTransferModal { get; set; }

      protected UploadMessageTransferModal MessageTransferModal { get; set; }

      protected bool ShowNavigation = false;
      protected bool ShowUserNavigation = false;
      protected string Username = string.Empty;
      protected string ProfileUrl = string.Empty;
      protected string SearchKeyword = string.Empty;

      protected override void OnInitialized()
      {
         NavigationManager.LocationChanged += HandleLocationChanged;
         UserSessionService.ServiceInitializedEventHandler += UserSessionStateChangedEventHandler;
         UserSessionService.UserLoggedInEventHandler += UserSessionStateChangedEventHandler;
         UserSessionService.UserLoggedOutEventHandler += UserSessionStateChangedEventHandler;
      }

      protected void HandleUserSessionStateChanged()
      {
         ShowUserNavigation = UserSessionService.Session.IsSome;
         Username = UserSessionService.Session.Match(
            () => "",
            session => session.Username);
         ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
         ShowNavigation = true;
         StateHasChanged();
      }

      protected async Task OnLogoutClicked()
      {
         await UserSessionService.LogoutAsync();
         await BrowserRepository.RecycleAsync();
         NavigationManager.NavigateTo("/");
      }

      protected void HandleLocationChanged(object sender, LocationChangedEventArgs e)
      {
         InvokeAsync(async () =>
         {
            await CollapseNavigationMenuAsync();
         });
      }

      protected void UserSessionStateChangedEventHandler(object sender, EventArgs _)
      {
         HandleUserSessionStateChanged();
      }

      protected async Task OnEncryptFileClicked()
      {
         FileTransferModal.Open();
         await CollapseNavigationMenuAsync();
      }

      protected async Task OnEncryptMessageClicked()
      {
         MessageTransferModal.Open();
         await CollapseNavigationMenuAsync();
      }

      protected async Task CollapseNavigationMenuAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CollapseNavBar");
      }

      protected void OnSearchClicked()
      {
         NavigationManager.NavigateTo($"/user/search?query={SearchKeyword}");
      }

      public void Dispose()
      {
         NavigationManager.LocationChanged -= HandleLocationChanged;
         UserSessionService.ServiceInitializedEventHandler -= UserSessionStateChangedEventHandler;
         UserSessionService.UserLoggedInEventHandler -= UserSessionStateChangedEventHandler;
         UserSessionService.UserLoggedOutEventHandler -= UserSessionStateChangedEventHandler;
         GC.SuppressFinalize(this);
      }
   }
}
