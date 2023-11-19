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

using System.Threading.Tasks;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Contracts.Features.Transfer;
using EasyMonads;
using Microsoft.AspNetCore.Components.Web;

namespace Crypter.Web.Shared.Transfer;

public partial class DownloadMessageTransferBase : DownloadTransferBase
{
   protected string Subject = string.Empty;
   protected string PlaintextMessage = string.Empty;
   protected long MessageSize = 0;

   private DownloadMessageHandler _downloadHandler;

   protected override async Task OnInitializedAsync()
   {
      await PrepareMessagePreviewAsync();
      FinishedLoading = true;
   }

   protected async Task PrepareMessagePreviewAsync()
   {
      _downloadHandler = TransferHandlerFactory.CreateDownloadMessageHandler(TransferHashId, UserType);
      var previewResponse = await _downloadHandler.DownloadPreviewAsync();
      previewResponse.DoRight(x =>
      {
         Subject = x.Subject;
         Created = x.CreationUTC.ToLocalTime();
         Expiration = x.ExpirationUTC.ToLocalTime();
         MessageSize = x.Size;
         SenderUsername = x.Sender;
         SpecificRecipient = !string.IsNullOrEmpty(x.Recipient);
      });

      ItemFound = previewResponse.IsRight;
   }

   protected async Task OnDecryptClickedAsync(MouseEventArgs _)
   {
      DecryptionInProgress = true;

      Maybe<byte[]> recipientPrivateKey = SpecificRecipient
         ? UserKeysService.PrivateKey
         : DeriveRecipientPrivateKeyFromUrlSeed();

      recipientPrivateKey.IfNone(() => ErrorMessage = "Invalid decryption key.");
      await recipientPrivateKey.IfSomeAsync(async x =>
      {
         _downloadHandler.SetRecipientInfo(x);

         await SetProgressMessage(_decryptingLiteral);
         var decryptionResponse = await _downloadHandler.DownloadCiphertextAsync();

         decryptionResponse.DoLeftOrNeither(
            x => HandleDownloadError(x),
            () => HandleDownloadError());

         decryptionResponse.DoRight(x =>
         {
            PlaintextMessage = x;
            DecryptionComplete = true;
         });
      });

      DecryptionInProgress = false;
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
            ErrorMessage = "Message not found";
            break;
         case DownloadTransferCiphertextError.UnknownError:
            ErrorMessage = "An error occurred";
            break;
         case DownloadTransferCiphertextError.InvalidRecipientProof:
            ErrorMessage = "Invalid decryption key";
            break;
      }
   }
}