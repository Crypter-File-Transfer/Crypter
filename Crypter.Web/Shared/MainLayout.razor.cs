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
using Crypter.ClientServices.Interfaces.Events;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class MainLayoutBase : LayoutComponentBase, IDisposable
   {
      [Inject]
      private IDeviceRepository<BrowserStorageLocation> BrowserRepository { get; set; }

      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Inject]
      private IUserContactsService UserContactsService { get; set; }

      [Inject]
      private IUserKeysService UserKeysService { get; set; }

      protected bool ServicesInitialized { get; set; }

      protected override async Task OnInitializedAsync()
      {
         await BrowserRepository.InitializeAsync();
         await UserSessionService.InitializeAsync();

         await UserSessionService.Session.IfSomeAsync(async x =>
         {
            await UserKeysService.InitializeAsync();
            await UserContactsService.InitializeAsync();
         });

         UserSessionService.UserLoggedInEventHandler += OnUserLoggedIn;
         UserSessionService.UserLoggedOutEventHandler += OnUserLoggedOut;

         ServicesInitialized = true;
      }

      private void OnUserLoggedIn(object sender, UserLoggedInEventArgs eventArgs)
      {
         ServicesInitialized = false;

         InvokeAsync(async () =>
         {
            await UserKeysService.PrepareUserKeysOnUserLoginAsync(eventArgs.Username, eventArgs.Password, eventArgs.RememberUser);
            await UserContactsService.InitializeAsync();
            ServicesInitialized = true;
            StateHasChanged();
         });
      }

      private void OnUserLoggedOut(object sender, EventArgs _)
      {
         UserKeysService.Recycle();
         UserContactsService.Recycle();
      }

      public void Dispose()
      {
         UserSessionService.UserLoggedInEventHandler -= OnUserLoggedIn;
         UserSessionService.UserLoggedOutEventHandler -= OnUserLoggedOut;
         GC.SuppressFinalize(this);
      }
   }
}
