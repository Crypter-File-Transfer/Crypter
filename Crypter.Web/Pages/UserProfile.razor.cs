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
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Web.Shared.Modal;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages;

public partial class UserProfile
{
    [Inject] private ICrypterApiClient CrypterApiService { get; set; } = null!;

    [Inject] private IUserSessionService UserSessionService { get; set; } = null!;

    [Parameter] public required string Username { get; set; }

    private UploadFileTransferModal? FileModal { get; set; }
    private UploadMessageTransferModal? MessageModal { get; set; }

    private bool _loading = true;
    private bool _isProfileAvailable;
    private string _alias = string.Empty;
    private string _about = string.Empty;
    private string _properUsername = string.Empty;
    private bool _allowsFiles;
    private bool _allowsMessages;
    private byte[] _userPublicKey = [];
    private bool _emailVerified;
    
    protected override async Task OnParametersSetAsync()
    {
        await PrepareUserProfileAsync();
        _loading = false;
    }

    private async Task PrepareUserProfileAsync()
    {
        bool isLoggedIn = await UserSessionService.IsLoggedInAsync();
        await CrypterApiService.User.GetUserProfileAsync(Username, isLoggedIn)
            .DoRightAsync(x =>
            {
                _isProfileAvailable = true;
                _alias = x.Alias;
                _about = x.About;
                _properUsername = x.Username;
                _allowsFiles = x.ReceivesFiles;
                _allowsMessages = x.ReceivesMessages;
                _userPublicKey = x.PublicKey;
                _emailVerified = x.EmailVerified;
            });
    }
}
