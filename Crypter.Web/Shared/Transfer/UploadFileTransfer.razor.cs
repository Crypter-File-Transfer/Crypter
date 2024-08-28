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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Web.Services;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualBasic;

namespace Crypter.Web.Shared.Transfer;

[SupportedOSPlatform("browser")]
public partial class UploadFileTransfer : IDisposable
{
    private IBrowserFile? _selectedFile;
    
    private long _maxStreamSizeMB = 0;
    private long _maxBufferSizeMB = 0;
    private string _dropClass = string.Empty;
    private const string DropzoneDrag = "dropzone-drag";
    private const string NoFileSelected = "No file selected.";

    protected override void OnInitialized()
    {
        TransmissionType = BrowserFunctions.BrowserSupportsRequestStreaming()
            ? TransferTransmissionType.Stream
            : TransferTransmissionType.Buffer;
        _maxStreamSizeMB = UploadSettings.MaximumStreamSizeMB * Convert.ToInt64(Math.Pow(10, 6));
        _maxBufferSizeMB = UploadSettings.MaximumBufferSizeMB * Convert.ToInt64(Math.Pow(10, 6));
    }

    private void HandleDragEnter()
    {
        _dropClass = DropzoneDrag;
    }

    private void HandleDragLeave()
    {
        _dropClass = string.Empty;
    }

    private void HandleFileInputChange(InputFileChangeEventArgs e)
    {
        _dropClass = string.Empty;
        ErrorMessage = string.Empty;

        IBrowserFile file = e.File;
        
        switch (TransmissionType)
        {
            case TransferTransmissionType.Stream when file.Size > _maxStreamSizeMB:
                ErrorMessage = $"The max file size is {UploadSettings.MaximumStreamSizeMB} MB.";
                return;
            case TransferTransmissionType.Buffer when file.Size > _maxBufferSizeMB:
                ErrorMessage = $"The max file size for this browser is {UploadSettings.MaximumBufferSizeMB} MB. Switch to a Chromium-based browser for files up to {UploadSettings.MaximumStreamSizeMB} MB.";
                return;
            default:
                _selectedFile = file;
                break;
        }
    }

    protected async Task OnEncryptClicked()
    {
        if (_selectedFile is null)
        {
            ErrorMessage = NoFileSelected;
            return;
        }

        EncryptionInProgress = true;
        ErrorMessage = string.Empty;
        
        await SetProgressMessageAsync("Encrypting file");

        UploadFileHandler fileUploader = TransferHandlerFactory.CreateUploadFileHandler(FileStreamOpener,
            _selectedFile.Name, _selectedFile.Size, _selectedFile.ContentType, ExpirationHours);

        SetHandlerUserInfo(fileUploader);

        Action<double>? progressUpdater = TransmissionType switch
        {
            TransferTransmissionType.Buffer => null,
            TransferTransmissionType.Stream => SetUploadPercentage,
            _ => null
        };

        Either<UploadTransferError, UploadHandlerResponse> uploadResponse = await fileUploader.UploadAsync(progressUpdater);

        if (TransmissionType == TransferTransmissionType.Stream)
        {
            await Task.Delay(400);
        }
        
        await HandleUploadResponse(uploadResponse);
        Dispose();
        return;

        Stream FileStreamOpener()
            => _selectedFile.OpenReadStream(_selectedFile.Size);
    }

    private async Task SetProgressMessageAsync(string message)
    {
        UploadStatusMessage = message;
        StateHasChanged();
        await Task.Delay(400);
    }

    private void SetUploadPercentage(double percentage)
    {
        UploadStatusPercent = percentage;
        InvokeAsync(StateHasChanged);
    }
    
    public void Dispose()
    {
        _selectedFile = null;
        EncryptionInProgress = false;
        _dropClass = string.Empty;
        UploadStatusPercent = null;
        GC.SuppressFinalize(this);
    }
}
