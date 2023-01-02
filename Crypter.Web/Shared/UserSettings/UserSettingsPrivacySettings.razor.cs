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
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Enums;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.UserSettings
{
   public class UserSettingsPrivacySettingsBase : ComponentBase
   {
      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Parameter]
      public UserVisibilityLevel ProfileVisibility { get; set; }

      [Parameter]
      public UserItemTransferPermission MessageTransferPermission { get; set; }

      [Parameter]
      public UserItemTransferPermission FileTransferPermission { get; set; }

      protected bool IsEditing;
      protected int ProfileVisibilityEdit;
      protected int MessageTransferPermissionEdit;
      protected int FileTransferPermissionEdit;

      protected override void OnParametersSet()
      {
         ProfileVisibilityEdit = (int)ProfileVisibility;
         MessageTransferPermissionEdit = (int)MessageTransferPermission;
         FileTransferPermissionEdit = (int)FileTransferPermission;
      }

      protected void OnEditClicked()
      {
         IsEditing = true;
      }

      protected void OnCancelClicked()
      {
         ProfileVisibilityEdit = (int)ProfileVisibility;
         MessageTransferPermissionEdit = (int)MessageTransferPermission;
         FileTransferPermissionEdit = (int)FileTransferPermission;
         IsEditing = false;
      }

      protected async Task OnSaveClickedAsync()
      {
         var request = new UpdatePrivacySettingsRequest(true, (UserVisibilityLevel)ProfileVisibilityEdit, (UserItemTransferPermission)MessageTransferPermissionEdit, (UserItemTransferPermission)FileTransferPermissionEdit);
         await CrypterApiService.UpdateUserPrivacySettingsAsync(request);

         ProfileVisibility = (UserVisibilityLevel)ProfileVisibilityEdit;
         MessageTransferPermission = (UserItemTransferPermission)MessageTransferPermissionEdit;
         FileTransferPermission = (UserItemTransferPermission)FileTransferPermissionEdit;
         IsEditing = false;
      }
   }
}
