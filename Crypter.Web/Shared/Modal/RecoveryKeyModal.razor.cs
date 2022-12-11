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
using Crypter.ClientServices.Interfaces.Models;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using Crypter.Web.Shared.Modal.Template;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class RecoveryKeyModalBase : ComponentBase
   {
      [Inject]
      protected IJSRuntime JSRuntime { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      protected IUserKeysService UserKeysService { get; set; }

      protected string RecoveryKey;

      protected ModalBehavior ModalBehaviorRef { get; set; }

      public async Task OpenAsync(Username username, Password password)
      {
         RecoveryKey = await UserKeysService.GetExistingRecoveryKeyAsync(username, password)
            .MatchAsync(
            () => "An error occurred",
            x => x.ToBase64String());

         ModalBehaviorRef.Open();
      }

      public async Task OpenAsync(Username username, VersionedPassword versionedPassword)
      {
         RecoveryKey = await UserKeysService.GetExistingRecoveryKeyAsync(username, versionedPassword)
            .MatchAsync(
            () => "An error occurred",
            x => x.ToBase64String());

         ModalBehaviorRef.Open();
      }

      protected async Task CopyRecoveryKeyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", new object[] { RecoveryKey, "recoveryKeyModalCopyTooltip" });
      }

      public async void OnAcknowledgedClickedAsync()
      {
         await CrypterApiService.ConsentToRecoveryKeyRisksAsync();
         ModalBehaviorRef.Close();
      }
   }
}
