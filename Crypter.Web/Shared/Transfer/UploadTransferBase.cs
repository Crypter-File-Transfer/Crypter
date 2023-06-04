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

using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public class UploadTransferBase : ComponentBase
   {
      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      [Inject]
      protected IUserKeysService UserKeysService { get; set; }

      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected Common.Client.Transfer.Models.TransferSettings UploadSettings { get; set; }

      [Inject]
      protected TransferHandlerFactory TransferHandlerFactory { get; set; }

      [Parameter]
      public Maybe<string> RecipientUsername { get; set; }

      [Parameter]
      public Maybe<byte[]> RecipientPublicKey { get; set; }

      [Parameter]
      public int ExpirationHours { get; set; }

      [Parameter]
      public EventCallback UploadCompletedEvent { get; set; }

      [CascadingParameter]
      public TransferSuccessModal ModalForAnonymousRecipient { get; set; }

      [CascadingParameter]
      public BasicModal ModalForUserRecipient { get; set; }

      protected bool EncryptionInProgress = false;
      protected string ErrorMessage = string.Empty;
      protected string UploadStatusMessage = string.Empty;

      private const string _unknownError = "An error occurred.";
      private const string _serverOutOfSpace = "Server is out of space. Try again later.";
      private const string _userNotFound = "User not found.";
      private const string _expirationRange = "Expiration must be between 1 and 24 hours.";
      protected const string _encryptingLiteral = "Encrypting";

      protected void SetHandlerUserInfo(IUserUploadHandler handler)
      {
         if (UserSessionService.Session.IsSome)
         {
            byte[] senderPrivateKey = UserKeysService.PrivateKey.Match(
               () => throw new Exception("Missing sender private key"),
               x => x);

            handler.SetSenderInfo(senderPrivateKey);
         }

         RecipientUsername.IfSome(x =>
         {
            byte[] recipientX25519PublicKey = RecipientPublicKey.Match(
               () => throw new Exception("Missing recipient public key"),
               x => x);

            handler.SetRecipientInfo(x, recipientX25519PublicKey);
         });
      }

      protected async Task HandleUploadResponse(Either<UploadTransferError, UploadHandlerResponse> uploadResponse)
      {
         uploadResponse.DoLeftOrNeither(HandleUploadError, () => HandleUploadError());

         await uploadResponse.DoRightAsync(async response =>
         {
            await UploadCompletedEvent.InvokeAsync();

#pragma warning disable CS8524
            string itemType = (response.ItemType) switch
            {
               TransferItemType.Message => "message",
               TransferItemType.File => "file"
            };
#pragma warning restore CS8524

            response.RecipientKeySeed.IfNone(() =>
            {
               ModalForUserRecipient.Open("Sent", $"Your {itemType} has been sent.", "Ok", Maybe<string>.None, Maybe<EventCallback<bool>>.None);
            });

            response.RecipientKeySeed.IfSome(x =>
            {
               string recipientKeySeed = Base64UrlEncoder.Encode(x);
               string downloadUrl = $"{NavigationManager.BaseUri}decrypt/{itemType}/{(int)response.UserType}/{response.TransferId}#{recipientKeySeed}";
               ModalForAnonymousRecipient.Open(downloadUrl, response.ExpirationHours, UploadCompletedEvent);
            });
         });
      }

      protected void HandleUploadError(UploadTransferError error = UploadTransferError.UnknownError)
      {
         switch (error)
         {
            case UploadTransferError.UnknownError:
               ErrorMessage = _unknownError;
               break;
            case UploadTransferError.InvalidRequestedLifetimeHours:
               ErrorMessage = _expirationRange;
               break;
            case UploadTransferError.RecipientNotFound:
               ErrorMessage = _userNotFound;
               break;
            case UploadTransferError.OutOfSpace:
               ErrorMessage = _serverOutOfSpace;
               break;
         }
      }
   }
}
