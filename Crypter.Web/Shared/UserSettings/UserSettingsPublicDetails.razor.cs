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
using Crypter.Common.Contracts.Features.UserSettings.ProfileSettings;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsPublicDetailsBase : ComponentBase
{
    [Inject] protected IUserProfileSettingsService UserProfileSettingsService { get; set; }

    protected string Alias { get; set; } = string.Empty;
    protected string AliasEdit { get; set; } = string.Empty;

    protected string About { get; set; } = string.Empty;
    protected string AboutEdit { get; set; } = string.Empty;

    protected bool IsDataReady { get; set; } = false;
    protected bool IsEditing { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        await UserProfileSettingsService.GetProfileSettingsAsync()
            .IfSomeAsync(x =>
            {
                Alias = x.Alias;
                AliasEdit = x.Alias;

                About = x.About;
                AboutEdit = x.About;
            });

        IsDataReady = true;
    }

    protected void OnEditClicked()
    {
        AliasEdit = Alias;
        AboutEdit = About;
        IsEditing = true;
    }

    protected void OnCancelClicked()
    {
        AliasEdit = Alias;
        AboutEdit = About;
        IsEditing = false;
    }

    protected async Task OnSaveClickedAsync()
    {
        var request = new ProfileSettings(AliasEdit, AboutEdit);
        await UserProfileSettingsService.SetProfileSettingsAsync(request)
            .DoRightAsync(x =>
            {
                Alias = x.Alias;
                About = x.About;
            });

        IsEditing = false;
    }
}
