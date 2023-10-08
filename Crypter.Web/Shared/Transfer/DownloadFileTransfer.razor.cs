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

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Web.Services;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared.Transfer
{
   [SupportedOSPlatform("browser")]
   public partial class DownloadFileTransferBase : DownloadTransferBase, IDisposable
   {
      [Inject]
      private IJSRuntime JSRuntime { get; set; }
      
      protected string FileName = string.Empty;
      protected string ContentType = string.Empty;
      protected long FileSize = 0;
      protected bool LocalDownloadInProgress { get; set; }

      private DownloadFileHandler _downloadHandler;

      protected override async Task OnInitializedAsync()
      {
         await PrepareFilePreviewAsync();
         FinishedLoading = true;
      }

      protected async Task PrepareFilePreviewAsync()
      {
         _downloadHandler = TransferHandlerFactory.CreateDownloadFileHandler(TransferHashId, UserType);
         var previewResponse = await _downloadHandler.DownloadPreviewAsync();
         previewResponse.DoRight(x =>
         {
            FileName = x.FileName;
            ContentType = x.ContentType;
            Created = x.CreationUTC.ToLocalTime();
            Expiration = x.ExpirationUTC.ToLocalTime();
            FileSize = x.Size;
            SenderUsername = x.Sender;
            SpecificRecipient = !string.IsNullOrEmpty(x.Recipient);
         });

         ItemFound = previewResponse.IsRight;
      }

      protected async Task OnDecryptClickedAsync(MouseEventArgs _)
      {
         BrowserDownloadFileService.Reset();
         DecryptionInProgress = true;

         Maybe<byte[]> recipientPrivateKey = SpecificRecipient
            ? UserKeysService.PrivateKey
            : DeriveRecipientPrivateKeyFromUrlSeed();

         recipientPrivateKey.IfNone(() => ErrorMessage = "Invalid decryption key");
         await recipientPrivateKey.IfSomeAsync(async x =>
         {
            _downloadHandler.SetRecipientInfo(x);

            await SetProgressMessage("Decrypting and downloading...");
            var decryptionResponse = await _downloadHandler.DownloadCiphertextAndOpenDecryptionStreamAsync();

            decryptionResponse.DoLeftOrNeither(
               HandleDownloadError,
               () => HandleDownloadError());

            await decryptionResponse.DoRightAsync(async decryptionStream =>
            {
               await JSRuntime.InvokeVoidAsync("sendStreamToServiceWorker", decryptionStream);
               DecryptionComplete = true;
            });
         });

         DecryptionInProgress = false;
         StateHasChanged();
      }

      protected async Task DownloadFileAsync()
      {
         LocalDownloadInProgress = true;
         StateHasChanged();
         await Task.Delay(400);

         BrowserDownloadFileService.Download();
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
            case DownloadTransferCiphertextError.InvalidRecipientProof:
               ErrorMessage = "Invalid decryption key";
               break;
         }
      }

      public void Dispose()
      {
         BrowserDownloadFileService.Reset();
         GC.SuppressFinalize(this);
      }
   }
}
