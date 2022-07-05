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

using Crypter.ClientServices.Transfer.Handlers;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Transfer;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class DownloadFileTransferBase : DownloadTransferBase
   {
      [Inject]
      protected IBrowserDownloadFileService BrowserDownloadFileService { get; set; }

      protected string FileName = string.Empty;
      protected string ContentType = string.Empty;
      protected List<byte[]> PlaintextBytes = null;
      protected int FileSize = 0;
      protected bool LocalDownloadInProgress { get; set; }

      private DownloadFileHandler _downloadHandler;

      protected override async Task OnInitializedAsync()
      {
         await PrepareFilePreviewAsync();
         FinishedLoading = true;
      }

      protected async Task PrepareFilePreviewAsync()
      {
         TransferUserType transferUserType = IsUserTransfer
            ? TransferUserType.User
            : TransferUserType.Anonymous;

         _downloadHandler = TransferHandlerFactory.CreateDownloadFileHandler(TransferId, transferUserType);
         var previewResponse = await _downloadHandler.DownloadPreviewAsync();
         previewResponse.DoRight(x =>
         {
            FileName = string.IsNullOrEmpty(x.FileName)
               ? "{ no file name }"
               : x.FileName;
            ContentType = x.ContentType;
            Created = x.CreationUTC.ToLocalTime();
            Expiration = x.ExpirationUTC.ToLocalTime();
            FileSize = x.Size;
            SenderUsername = x.Sender;
            SpecificRecipient = !string.IsNullOrEmpty(x.Recipient);
         });

         ItemFound = previewResponse.IsRight;
      }

      protected async Task OnDecryptClickedAsync()
      {
         DecryptionInProgress = true;

         Maybe<PEMString> recipientPrivateKey = SpecificRecipient
            ? UserKeysService.X25519PrivateKey
            : ValidateAndDecodeUserProvidedDecryptionKey(UserProvidedDecryptionKey);

         recipientPrivateKey.IfNone(() => ErrorMessage = "Invalid decryption key.");
         await recipientPrivateKey.IfSomeAsync(async x =>
         {
            _downloadHandler.SetRecipientInfo(x);

            await SetProgressMessage(_downloadingLiteral);
            var showDecryptingMessage = Maybe<Func<Task>>.From(() => SetProgressMessage(_decryptingLiteral));
            var showVerifyingMessage = Maybe<Func<Task>>.From(() => SetProgressMessage(_verifyingLiteral));
            var decryptionResponse = await _downloadHandler.DownloadCiphertextAsync(showDecryptingMessage, showVerifyingMessage);

            decryptionResponse.DoLeftOrNeither(
            x => HandleDownloadError(x),
            () => HandleDownloadError());

            decryptionResponse.DoRight(x =>
            {
               PlaintextBytes = new List<byte[]> { x };
               DecryptionComplete = true;
            });
         });

         DecryptionInProgress = false;
      }

      protected async Task DownloadFileAsync()
      {
         LocalDownloadInProgress = true;
         StateHasChanged();
         await Task.Delay(400);

         await BrowserDownloadFileService.ResetDownloadAsync();
         await BrowserDownloadFileService.DownloadFileAsync(FileName, ContentType, PlaintextBytes);

         LocalDownloadInProgress = false;
         StateHasChanged();
      }

      protected async Task SetProgressMessage(string message)
      {
         DecryptionStatusMessage = message;
         StateHasChanged();
         await Task.Delay(400);
      }

      private void HandleDownloadError(DownloadTransferCiphertextError error = DownloadTransferCiphertextError.UnknownError)
      {
         switch (error)
         {
            case DownloadTransferCiphertextError.NotFound:
               ErrorMessage = "File not found";
               break;
            case DownloadTransferCiphertextError.UnknownError:
               ErrorMessage = "An error occurred";
               break;
         }
      }
   }
}
