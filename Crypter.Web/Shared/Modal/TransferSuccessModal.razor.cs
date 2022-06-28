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

using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class TranferSuccessModalBase : ComponentBase
   {
      [Inject]
      private IJSRuntime JSRuntime { get; set; }

      protected string DownloadUrl { get; set; }

      protected Base64String DecryptionKey { get; set; }

      protected int ExpirationHours { get; set; }

      protected Maybe<EventCallback> ModalClosedCallback { get; set; }

      protected bool Show = false;
      protected string ModalDisplay = "none;";
      protected string ModalClass = "";

      public void Open(string downloadUrl, PEMString recipientX25519PrivateKey, int expirationHours, Maybe<EventCallback> modalClosedCallback)
      {
         DownloadUrl = downloadUrl;
         DecryptionKey = Base64String.From(Convert.ToBase64String(
            Encoding.UTF8.GetBytes(recipientX25519PrivateKey.Value)));
         ExpirationHours = expirationHours;
         ModalClosedCallback = modalClosedCallback;

         ModalDisplay = "block;";
         ModalClass = "Show";
         Show = true;
         StateHasChanged();
      }

      public async Task CloseAsync()
      {
         ModalDisplay = "none";
         ModalClass = "";
         Show = false;
         StateHasChanged();

         await ModalClosedCallback.IfSomeAsync(async x => await x.InvokeAsync());
      }

      protected async Task CopyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", DecryptionKey.Value);
      }
   }
}
