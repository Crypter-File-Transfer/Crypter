﻿/*
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

      [Parameter]
      public string UploadType { get; set; }

      [Parameter]
      public Guid ItemId { get; set; }

      [Parameter]
      public string RecipientX25519PrivateKey { get; set; }

      [Parameter]
      public EventCallback<string> UploadTypeChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> ItemIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientX25519PrivateKeyChanged { get; set; }

      [Parameter]
      public EventCallback ModalClosedCallback { get; set; }

      [Parameter]
      public int RequestedExpirationHours { get; set; }

      public string ModalDisplay = "none;";
      public string ModalClass = "";
      public bool ShowBackdrop = false;

      public void Open()
      {
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
         await ModalClosedCallback.InvokeAsync();
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
