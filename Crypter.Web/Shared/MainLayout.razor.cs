﻿/*
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
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.Web.Shared.Modal;
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

      public BasicModal BasicModal { get; protected set; }

      public TransferSuccessModal TransferSuccessModal { get; protected set; }

      public RecoveryKeyModal RecoveryKeyModal { get; protected set; }

      protected bool ServicesInitialized { get; set; }

      protected override async Task OnInitializedAsync()
      {
         UserSessionService.UserLoggedInEventHandler += ShowRecoveryKeyModalAsync;
         await BrowserRepository.InitializeAsync();
         ServicesInitialized = true;
      }

      private async void ShowRecoveryKeyModalAsync(object sender, UserLoggedInEventArgs args)
      {
         if (args.ShowRecoveryKeyModal)
         {
            await RecoveryKeyModal.OpenAsync(args.Username, args.Password);
         }
      }

      public void Dispose()
      {
         UserSessionService.UserLoggedInEventHandler -= ShowRecoveryKeyModalAsync;
         GC.SuppressFinalize(this);
      }
   }
}
