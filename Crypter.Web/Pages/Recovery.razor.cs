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
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Infrastructure;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public class RecoveryBase : ComponentBase
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IUserRecoveryService UserRecoveryService { get; set; }

      private string RecoveryCode;

      private string RecoverySignature;

      protected bool RecoveryKeySwitch = true;

      protected Username Username { get; set; }
      protected string NewPassword { get; set; }
      protected string NewPasswordConfirm { get; set; }
      protected string RecoveryKeyInput { get; set; }

      protected bool RecoverySucceeded { get; set; }

      protected string RecoveryErrorMessage { get; set; }
      protected string RecoveryKeyErrorMessage { get; set; }

      protected override void OnInitialized()
      {
         Uri uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
         Dictionary<string, StringValues> queryParameters = QueryHelpers.ParseQuery(uri.Query);

         bool validPageLanding = queryParameters.ContainsKey("username")
            && queryParameters.ContainsKey("code")
            && queryParameters.ContainsKey("signature");

         if (!validPageLanding)
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         string decodedUsername = UrlSafeEncoder.DecodeStringUrlSafe(queryParameters["username"]);
         if (Username.TryFrom(decodedUsername, out Username validUsername))
         {
            Username = validUsername;
         }
         else
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         RecoveryCode = queryParameters["code"];
         RecoverySignature = queryParameters["signature"];
      }

      public async Task SubmitRecoveryAsync()
      {
         if (NewPassword != NewPasswordConfirm)
         {
            RecoveryErrorMessage = "Passwords do not match.";
            return;
         }

         if (!Password.TryFrom(NewPassword, out  Password validPassword))
         {
            RecoveryErrorMessage = "Invalid password.";
            return;
         }

         Either<SubmitRecoveryError, Maybe<RecoveryKey>> recoveryResult = RecoveryKeySwitch
            ? await RecoveryKey.FromBase64String(RecoveryKeyInput)
               .ToEither(SubmitRecoveryError.WrongRecoveryKey)
               .BindAsync(async x => await UserRecoveryService.SubmitRecoveryRequestAsync(RecoveryCode, RecoverySignature, Username, validPassword, x))
            : await UserRecoveryService.SubmitRecoveryRequestAsync(RecoveryCode, RecoverySignature, Username, validPassword, Maybe<RecoveryKey>.None);

#pragma warning disable CS8524
         recoveryResult.DoLeftOrNeither(
            errorCode => RecoveryErrorMessage = errorCode switch
            {
               SubmitRecoveryError.UnknownError => "An unknown error occurred.",
               SubmitRecoveryError.InvalidUsername => "Invalid username.",
               SubmitRecoveryError.RecoveryNotFound => "This recovery link is expired. Request a new recovery link and try again.",
               SubmitRecoveryError.WrongRecoveryKey => "The recovery key you provided is invalid.",
               SubmitRecoveryError.PasswordHashFailure => "A cryptographic error occurred while securing your new password. This device or browser may not be supported.",
            },
            () => RecoveryErrorMessage = "An unknown error occurred.");
#pragma warning restore CS8524

         RecoverySucceeded = recoveryResult.IsRight;
      }
   }
}
