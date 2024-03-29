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

using System.Collections.Generic;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.Contacts;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages.Authenticated;

public partial class UserContacts
{
    [Inject] private IUserContactsService UserContactsService { get; init; } = null!;

    private bool _loading = true;

    private IReadOnlyCollection<UserContact> _contacts = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && await UserSessionService.IsLoggedInAsync())
        {
            _contacts = await UserContactsService.GetContactsAsync();
            StateHasChanged();
        }
    }

    private static string GetDisplayName(string username, string alias)
    {
        return string.IsNullOrEmpty(alias)
            ? username
            : $"{alias} ({username})";
    }

    private async Task RemoveContactAsync(string contactUsername)
    {
        await UserContactsService.RemoveContactAsync(contactUsername);
        _contacts = await UserContactsService.GetContactsAsync();
    }
}
