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
using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared;

public partial class Navigation : IDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; init; } = null!;

    [Inject] private NavigationManager NavigationManager { get; init; } = null!;

    [Inject] private IUserSessionService UserSessionService { get; init; } = null!;

    [Inject] private IDeviceRepository<BrowserStorageLocation> BrowserRepository { get; init; } = null!;

    private UploadFileTransferModal FileTransferModal { get; set; } = null!;

    private UploadMessageTransferModal MessageTransferModal { get; set; } = null!;

    private bool _showNavigation;
    private bool _showUserNavigation;
    private string _username = string.Empty;
    private string _profileUrl = string.Empty;
    private string _searchKeyword = string.Empty;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
        UserSessionService.ServiceInitializedEventHandler += UserSessionStateChangedEventHandler;
        UserSessionService.UserLoggedInEventHandler += UserSessionStateChangedEventHandler;
        UserSessionService.UserLoggedOutEventHandler += UserSessionStateChangedEventHandler;
    }

    private void HandleUserSessionStateChanged()
    {
        _showUserNavigation = UserSessionService.Session.IsSome;
        _username = UserSessionService.Session.Match(
            () => "",
            session => session.Username);
        _profileUrl = $"{NavigationManager.BaseUri}user/profile/{_username}";
        _showNavigation = true;
        StateHasChanged();
    }

    private async Task OnLogoutClicked()
    {
        await UserSessionService.LogoutAsync();
        await BrowserRepository.RecycleAsync();
        NavigationManager.NavigateTo("/");
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        InvokeAsync(async () => { await CollapseNavigationMenuAsync(); });
    }

    private void UserSessionStateChangedEventHandler(object? sender, EventArgs _)
    {
        HandleUserSessionStateChanged();
    }

    private async Task OnEncryptFileClicked()
    {
        FileTransferModal.Open();
        await CollapseNavigationMenuAsync();
    }

    private async Task OnEncryptMessageClicked()
    {
        MessageTransferModal.Open();
        await CollapseNavigationMenuAsync();
    }

    private async Task CollapseNavigationMenuAsync()
    {
        await JsRuntime.InvokeVoidAsync("Crypter.CollapseNavBar");
    }

    private void OnSearchClicked()
    {
        NavigationManager.NavigateTo($"/user/search?query={_searchKeyword}");
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
        UserSessionService.ServiceInitializedEventHandler -= UserSessionStateChangedEventHandler;
        UserSessionService.UserLoggedInEventHandler -= UserSessionStateChangedEventHandler;
        UserSessionService.UserLoggedOutEventHandler -= UserSessionStateChangedEventHandler;
        GC.SuppressFinalize(this);
    }
}
