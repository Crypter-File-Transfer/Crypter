﻿/*
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

using System.Runtime.Versioning;
using System.Threading.Tasks;
using Crypter.Web.Shared.Modal.Template;
using Crypter.Web.Shared.Transfer;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.Modal;

[SupportedOSPlatform("browser")]
public partial class UploadFileTransferModal
{
    [Parameter] public required string InstanceId { get; set; }

    [Parameter] public required Maybe<string> RecipientUsername { get; set; }

    [Parameter] public required Maybe<byte[]> RecipientPublicKey { get; set; }

    [Parameter] public required EventCallback ModalClosedCallback { get; set; }

    private ModalBehavior _modalBehaviorRef = null!;
    private UploadFileTransfer _uploadComponent = null!;

    private int _expirationHours;

    public void Open()
    {
        _modalBehaviorRef.Open();
    }

    public async Task CloseAsync()
    {
        _uploadComponent.Dispose();
        await ModalClosedCallback.InvokeAsync();
        _modalBehaviorRef.Close();
    }
}
