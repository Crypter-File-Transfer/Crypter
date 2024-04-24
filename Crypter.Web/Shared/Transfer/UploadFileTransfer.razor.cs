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
using System.IO;
using System.Threading.Tasks;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using EasyMonads;
using Microsoft.AspNetCore.Components.Forms;

namespace Crypter.Web.Shared.Transfer;

public partial class UploadFileTransfer : IDisposable
{
    private IBrowserFile? _selectedFile;
    
    private long _maxFileSizeBytes = 0;
    private string _dropClass = string.Empty;
    private const string DropzoneDrag = "dropzone-drag";
    private const string NoFileSelected = "No file selected.";

    protected override void OnInitialized()
    {
        _maxFileSizeBytes = UploadSettings.MaximumTransferSizeMiB * (long)Math.Pow(2, 20);
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

        if (file.Size > _maxFileSizeBytes)
        {
            ErrorMessage = $"The max file size is {UploadSettings.MaximumTransferSizeMiB} MB.";
            return;
        }

        _selectedFile = file;
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

        await SetProgressMessage("Encrypting file");

        UploadFileHandler fileUploader = TransferHandlerFactory.CreateUploadFileHandler(FileStreamOpener,
            _selectedFile.Name, _selectedFile.Size, _selectedFile.ContentType, ExpirationHours);

        SetHandlerUserInfo(fileUploader);
        Either<UploadTransferError, UploadHandlerResponse> uploadResponse = await fileUploader.UploadAsync();
        await HandleUploadResponse(uploadResponse);
        Dispose();
        return;

        Stream FileStreamOpener()
            => _selectedFile.OpenReadStream(_selectedFile.Size);
    }

    private async Task SetProgressMessage(string message)
    {
        UploadStatusMessage = message;
        StateHasChanged();
        await Task.Delay(400);
    }

    public void Dispose()
    {
        _selectedFile = null;
        EncryptionInProgress = false;
        _dropClass = string.Empty;
        GC.SuppressFinalize(this);
    }
}
