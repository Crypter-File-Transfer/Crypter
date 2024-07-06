/*
 * Copyright (C) 2024 Crypter File Transfer
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

public partial class DownloadMessageTransfer
{
    private string _subject = string.Empty;
    private string _plaintextMessage = string.Empty;
    private long _messageSize = 0;

    private DownloadMessageHandler? _downloadHandler;

    protected override async Task OnInitializedAsync()
    {
        await PrepareMessagePreviewAsync();
        FinishedLoading = true;
    }

    private async Task PrepareMessagePreviewAsync()
    {
        _downloadHandler = TransferHandlerFactory.CreateDownloadMessageHandler(TransferHashId, UserType);
        Either<TransferPreviewError, MessageTransferPreviewResponse> previewResponse = await _downloadHandler.DownloadPreviewAsync();
        previewResponse.DoRight(x =>
        {
            _subject = x.Subject;
            Created = x.CreationUTC.ToLocalTime();
            Expiration = x.ExpirationUTC.ToLocalTime();
            _messageSize = x.Size;
            SenderUsername = x.Sender;
            SpecificRecipient = !string.IsNullOrEmpty(x.Recipient);
        });

        ItemFound = previewResponse.IsRight;
    }

    private async Task OnDecryptClickedAsync(MouseEventArgs _)
    {
        if (_downloadHandler is null)
        {
            ErrorMessage = "Download handler not assigned.";
            return;
        }

        await FileSaverService.UnregisterServiceWorkerAsync();
        
        DecryptionInProgress = true;

        Maybe<byte[]> recipientPrivateKey = SpecificRecipient
            ? UserKeysService.PrivateKey
            : DeriveRecipientPrivateKeyFromUrlSeed();

        recipientPrivateKey.IfNone(() => ErrorMessage = "Invalid decryption key.");
        await recipientPrivateKey.IfSomeAsync(async privateKey =>
        {
            _downloadHandler.SetRecipientInfo(privateKey);

            await SetProgressMessage(DecryptingLiteral);
            Either<DownloadTransferCiphertextError, string> decryptionResponse = await _downloadHandler.DownloadCiphertextAsync();

            decryptionResponse.DoLeftOrNeither(
                HandleDownloadError,
                () => HandleDownloadError());

            decryptionResponse.DoRight(x =>
            {
                _plaintextMessage = x;
                DecryptionComplete = true;
            });
        });

        DecryptionInProgress = false;
        StateHasChanged();
    }

    private async Task SetProgressMessage(string message)
    {
        DecryptionStatusMessage = message;
        StateHasChanged();
        await Task.Delay(400);
    }

    private void HandleDownloadError(
        DownloadTransferCiphertextError error = DownloadTransferCiphertextError.UnknownError)
    {
#pragma warning disable CS8524
        ErrorMessage = error switch
#pragma warning restore CS8524
        {
            DownloadTransferCiphertextError.NotFound => "Message not found",
            DownloadTransferCiphertextError.UnknownError => "An error occurred",
            DownloadTransferCiphertextError.InvalidRecipientProof => "Invalid decryption key",
        };
    }
}
