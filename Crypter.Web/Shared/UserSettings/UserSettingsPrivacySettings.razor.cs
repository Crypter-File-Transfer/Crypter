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
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.PrivacySettings;
using Crypter.Common.Enums;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsPrivacySettings
{
    [Inject] private IUserPrivacySettingsService UserPrivacySettingsService { get; set; }

    private UserVisibilityLevel _profileVisibility;
    private int _profileVisibilityEdit;

    private UserItemTransferPermission _messageTransferPermission;
    private int _messageTransferPermissionEdit;

    private UserItemTransferPermission _fileTransferPermission;
    private int _fileTransferPermissionEdit;

    private bool _isDataReady;
    private bool _isEditing;

    protected override async Task OnInitializedAsync()
    {
        await UserPrivacySettingsService.GetPrivacySettingsAsync()
            .IfSomeAsync(x =>
            {
                _profileVisibility = x.VisibilityLevel;
                _profileVisibilityEdit = (int)_profileVisibility;

                _messageTransferPermission = x.MessageTransferPermission;
                _messageTransferPermissionEdit = (int)_messageTransferPermission;

                _fileTransferPermission = x.FileTransferPermission;
                _fileTransferPermissionEdit = (int)_fileTransferPermission;
            });

        _isDataReady = true;
    }

    private void OnEditClicked()
    {
        _isEditing = true;
    }

    private void OnCancelClicked()
    {
        _profileVisibilityEdit = (int)_profileVisibility;
        _messageTransferPermissionEdit = (int)_messageTransferPermission;
        _fileTransferPermissionEdit = (int)_fileTransferPermission;
        _isEditing = false;
    }

    private async Task OnSaveClickedAsync()
    {
        var request = new PrivacySettings(true, (UserVisibilityLevel)_profileVisibilityEdit,
            (UserItemTransferPermission)_messageTransferPermissionEdit,
            (UserItemTransferPermission)_fileTransferPermissionEdit);
        await UserPrivacySettingsService.UpdatePrivacySettingsAsync(request)
            .DoRightAsync(x =>
            {
                _profileVisibility = x.VisibilityLevel;
                _profileVisibilityEdit = (int)_profileVisibility;

                _messageTransferPermission = x.MessageTransferPermission;
                _messageTransferPermissionEdit = (int)_messageTransferPermission;

                _fileTransferPermission = x.FileTransferPermission;
                _fileTransferPermissionEdit = (int)_fileTransferPermission;
            });

        _isEditing = false;
    }
}
