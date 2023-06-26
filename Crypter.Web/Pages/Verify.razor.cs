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

using System.Collections.Specialized;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Web.Helpers;
using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages
{
   public partial class VerifyBase : ComponentBase
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ICrypterApiClient CrypterApiService { get; set; }

      protected EmailVerificationParams EmailVerificationParams = new();

      protected bool EmailVerificationInProgress = true;
      protected bool EmailVerificationSuccess = false;

      protected override async Task OnInitializedAsync()
      {
         ParseVerificationParamsFromUri();
         await VerifyEmailAddressAsync();
      }

      protected void ParseVerificationParamsFromUri()
      {
         NameValueCollection queryParameters = NavigationManager.GetQueryParameters();
         EmailVerificationParams.Code = queryParameters["code"];
         EmailVerificationParams.Signature = queryParameters["signature"];
      }

      protected async Task VerifyEmailAddressAsync()
      {
         var verificationResponse = await CrypterApiService.UserSetting.VerifyUserEmailAddressAsync(
            new VerifyEmailAddressRequest(EmailVerificationParams.Code, EmailVerificationParams.Signature));

         EmailVerificationSuccess = verificationResponse.IsRight;
         EmailVerificationInProgress = false;
      }
   }
}
