/*
 * Copyright (C) 2025 Crypter File Transfer
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
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Crypter.Common.Client.Transfer.Handlers;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using EasyMonads;

namespace Crypter.Web.Shared.Transfer;

public partial class UploadMessageTransfer : IDisposable
{
    protected string MessageSubject = string.Empty;
    protected string MessageBody = string.Empty;
    protected const int _maxMessageLength = 1024;

    protected async Task OnEncryptClicked()
    {
        EncryptionInProgress = true;
        ErrorMessage = string.Empty;

        await SetProgressMessageAsync("Encrypting message");
        UploadMessageHandler messageUploader = TransferHandlerFactory.CreateUploadMessageHandler(MessageSubject, MessageBody, ExpirationHours);

        SetHandlerUserInfo(messageUploader);
        Either<UploadTransferError, UploadHandlerResponse> uploadResponse = await messageUploader.UploadAsync();
        await HandleUploadResponseAsync(uploadResponse);
        Dispose();
    }

    protected async Task SetProgressMessageAsync(string message)
    {
        UploadStatusMessage = message;
        StateHasChanged();
        await Task.Delay(400);
    }

    public void Dispose()
    {
        MessageSubject = string.Empty;
        MessageBody = string.Empty;
        EncryptionInProgress = false;
        GC.SuppressFinalize(this);
    }
}
