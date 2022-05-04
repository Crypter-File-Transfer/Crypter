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

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class TranferSuccessModalBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      protected string UploadType { get; set; }

      protected Guid ItemId { get; set; }

      protected string RecipientX25519PrivateKey { get; set; }

      protected int RequestedExpirationHours { get; set; }

      protected EventCallback OnClosed { get; set; }

      public string ModalDisplay = "none;";
      public string ModalClass = "";
      public bool ShowBackdrop = false;

      public void Open(string uploadType, Guid itemId, string recipientX25519PrivateKey, int requestedExpirationHours, EventCallback onClosed)
      {
         UploadType = uploadType;
         ItemId = itemId;
         RecipientX25519PrivateKey = recipientX25519PrivateKey;
         RequestedExpirationHours = requestedExpirationHours;
         OnClosed = onClosed;

         ModalDisplay = "block;";
         ModalClass = "Show";
         ShowBackdrop = true;
         StateHasChanged();
      }

      public async Task CloseAsync()
      {
         ModalDisplay = "none";
         ModalClass = "";
         ShowBackdrop = false;
         StateHasChanged();
         await OnClosed.InvokeAsync();
      }

      protected string GetDownloadLink()
      {
         return $"{NavigationManager.BaseUri}decrypt/{UploadType}/{ItemId}";
      }

      protected async Task CopyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", RecipientX25519PrivateKey);
      }
   }
}
