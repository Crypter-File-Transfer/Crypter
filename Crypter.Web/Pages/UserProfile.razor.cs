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
using Crypter.Common.Primitives;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserProfileBase : ComponentBase
   {
      [Inject]
      private ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Parameter]
      public string Username { get; set; }

      protected UploadFileTransferModal FileModal { get; set; }
      protected UploadMessageTransferModal MessageModal { get; set; }

      protected bool Loading;
      protected bool IsProfileAvailable;
      protected string Alias;
      protected string About;
      protected string ProperUsername;
      protected bool AllowsFiles;
      protected bool AllowsMessages;
      protected PEMString UserX25519PublicKey;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         await PrepareUserProfileAsync();
         Loading = false;
      }

      protected async Task PrepareUserProfileAsync()
      {
         bool isLoggedIn = await UserSessionService.IsLoggedInAsync();
         var response = await CrypterApiService.GetUserProfileAsync(Username, isLoggedIn);
         response.DoRight(x =>
         {
            Alias = x.Result.Alias;
            About = x.Result.About;
            ProperUsername = x.Result.Username;
            AllowsFiles = x.Result.ReceivesFiles;
            AllowsMessages = x.Result.ReceivesMessages;
            UserX25519PublicKey = PEMString.From(
               Encoding.UTF8.GetString(Convert.FromBase64String(x.Result.PublicDHKey)));
         });

         IsProfileAvailable = response.Match(
            false,
            right => !string.IsNullOrEmpty(right.Result.PublicDHKey));
      }
   }
}
