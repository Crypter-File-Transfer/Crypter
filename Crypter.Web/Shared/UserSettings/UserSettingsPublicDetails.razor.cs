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
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.ProfileSettings;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsPublicDetails
{
    [Inject] private IUserProfileSettingsService UserProfileSettingsService { get; init; } = null!;

    private string _alias = string.Empty;
    private string _aliasEdit = string.Empty;

    private string _about = string.Empty;
    private string _aboutEdit = string.Empty;

    private bool _loading = true;
    private bool _isEditing;

    protected override async Task OnInitializedAsync()
    {
        await UserProfileSettingsService.GetProfileSettingsAsync()
            .IfSomeAsync(x =>
            {
                _alias = x.Alias;
                _aliasEdit = x.Alias;

                _about = x.About;
                _aboutEdit = x.About;
            });

        _loading = false;
    }

    private void OnEditClicked()
    {
        _aliasEdit = _alias;
        _aboutEdit = _about;
        _isEditing = true;
    }

    private void OnCancelClicked()
    {
        _aliasEdit = _alias;
        _aboutEdit = _about;
        _isEditing = false;
    }

    private async Task OnSaveClickedAsync()
    {
        ProfileSettings request = new ProfileSettings(_aliasEdit, _aboutEdit);
        await UserProfileSettingsService.SetProfileSettingsAsync(request)
            .DoRightAsync(x =>
            {
                _alias = x.Alias;
                _about = x.About;
            });

        _isEditing = false;
    }
}
