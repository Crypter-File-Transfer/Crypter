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

using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public class ReAuthenticationModalBase : ComponentBase
   {
      [Inject]
      private IAuthenticationService AuthenticationService { get; set; }

      [Parameter]
      public EventCallback<bool> ModalClosedCallback { get; set; }

      protected string Password { get; set; } = null;
      protected bool LoginFailed { get; set; } = false;

      protected string ModalDisplay = "none;";
      protected string ModalClass = "";
      protected bool ShowBackdrop = false;

      public void Open()
      {
         ModalDisplay = "block;";
         ModalClass = "Show";
         ShowBackdrop = true;
         StateHasChanged();
      }

      public async Task<bool> PerformReauthentication()
      {
         if (string.IsNullOrEmpty(Password))
         {
            return false;
         }

         return await AuthenticationService.ReauthenticateAsync(Password);
      }

      public async Task SubmitUnlockAsync()
      {
         LoginFailed = false;

         if (await PerformReauthentication())
         {
            ModalDisplay = "none";
            ModalClass = "";
            ShowBackdrop = false;
            await ModalClosedCallback.InvokeAsync();
            return;
         }

         LoginFailed = true;
      }

      public async Task OnLogoutClicked()
      {
         await AuthenticationService.LogoutAsync();
         ModalDisplay = "none";
         ModalClass = "";
         ShowBackdrop = false;
         await ModalClosedCallback.InvokeAsync();
      }
   }
}