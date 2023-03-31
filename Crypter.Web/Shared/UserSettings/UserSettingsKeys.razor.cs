/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Events;
using Crypter.Common.Monads;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.UserSettings
{
   public partial class UserSettingsKeysBase : ComponentBase, IDisposable
   {
      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Inject]
      private IUserKeysService UserKeysService { get; set; }

      [Inject]
      private IUserRecoveryService UserRecoveryService { get; set; }

      [Inject]
      protected IJSRuntime JSRuntime { get; set; }

      protected PasswordModal PasswordModal { get; set; }

      protected string PrivateKey;
      protected string RecoveryKey;

      protected override void OnInitialized()
      {
         PrivateKey = UserKeysService.PrivateKey.Match(
            () => "",
            Convert.ToHexString);

         RecoveryKey = string.Empty;

         UserSessionService.UserPasswordTestSuccessEventHandler += OnPasswordTestSuccess;
      }

      private async void OnPasswordTestSuccess(object sender, UserPasswordTestSuccessEventArgs args)
      {
         RecoveryKey = await UserKeysService.MasterKey
            .BindAsync(async masterKey => await UserRecoveryService.DeriveRecoveryKeyAsync(masterKey, args.Username, args.Password))
            .MatchAsync(
               () => "An error occurred",
               x => x.ToBase64String());

         await InvokeAsync(StateHasChanged);
      }

      protected async Task CopyRecoveryKeyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", new object[] { RecoveryKey, "recoveryKeyCopyTooltip" });
      }

      public void Dispose()
      {
         UserSessionService.UserPasswordTestSuccessEventHandler -= OnPasswordTestSuccess;
         GC.SuppressFinalize(this);
      }
   }
}
