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

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Crypto.Common.StreamEncryption;
using Crypter.Web.Services;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared.Transfer;

[SupportedOSPlatform("browser")]
public partial class DownloadFileTransfer :  IDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; init; } = null!;

    [Inject]
    private IStreamSaverService StreamSaverService { get; init; } = null!;
    
    private string _fileName = string.Empty;
    private string _contentType = string.Empty;
    private long _fileSize = 0;
    private bool _localDownloadInProgress;

    private DownloadFileHandler? _downloadHandler;

    protected override async Task OnInitializedAsync()
    {
        await PrepareFilePreviewAsync();
        FinishedLoading = true;
    }

    private async Task PrepareFilePreviewAsync()
    {
        _downloadHandler = TransferHandlerFactory.CreateDownloadFileHandler(TransferHashId, UserType);
        Either<TransferPreviewError, FileTransferPreviewResponse> previewResponse = await _downloadHandler.DownloadPreviewAsync();
        previewResponse.DoRight(x =>
        {
            _fileName = x.FileName;
            _contentType = x.ContentType;
            Created = x.CreationUTC.ToLocalTime();
            Expiration = x.ExpirationUTC.ToLocalTime();
            _fileSize = x.Size;
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
        
        BrowserDownloadFileService.Reset();
        DecryptionInProgress = true;

        Maybe<byte[]> recipientPrivateKey = SpecificRecipient
            ? UserKeysService.PrivateKey
            : DeriveRecipientPrivateKeyFromUrlSeed();

        recipientPrivateKey.IfNone(() => ErrorMessage = "Invalid decryption key");
        await recipientPrivateKey.IfSomeAsync(async privateKey =>
        {
            _downloadHandler.SetRecipientInfo(privateKey);

            await SetProgressMessage(DecryptingLiteral);
            Either<DownloadTransferCiphertextError, DecryptionStream> decryptionResponse = await _downloadHandler.DownloadCiphertextAsync();

            await decryptionResponse
                .DoRightAsync(async decryptionStream =>
                {
                    await StreamSaverService.SaveFileAsync(decryptionStream, _fileName, _contentType, null);
                    //await JSRuntime.InvokeVoidAsync("sendStreamToServiceWorker", decryptionStream);
                    DecryptionComplete = true;
                    await decryptionStream.DisposeAsync();
                })
                .DoLeftOrNeitherAsync(
                    HandleDownloadError,
                () => HandleDownloadError());
        });

        DecryptionInProgress = false;
        StateHasChanged();
    }

    private async Task DownloadFileAsync()
    {
        _localDownloadInProgress = true;
        StateHasChanged();
        await Task.Delay(400);

        BrowserDownloadFileService.Download();
        _localDownloadInProgress = false;
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
            DownloadTransferCiphertextError.NotFound => "File not found",
            DownloadTransferCiphertextError.UnknownError => "An error occurred",
            DownloadTransferCiphertextError.InvalidRecipientProof => "Invalid decryption key"
        };
    }

    public void Dispose()
    {
        BrowserDownloadFileService.Reset();
        GC.SuppressFinalize(this);
    }
}
