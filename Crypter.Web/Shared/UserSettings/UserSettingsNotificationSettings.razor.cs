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
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsNotificationSettings : IDisposable
{
    [Inject] protected ICrypterApiClient CrypterApiService { get; init; } = null!;

    [Inject] protected IUserContactInfoSettingsService UserContactInfoSettingsService { get; init; } = null!;

    [Inject] protected IUserNotificationSettingsService UserNotificationSettingsService { get; init; } = null!;

    private bool _emailAddressVerified;

    private bool _enableTransferNotifications;
    private bool _enableTransferNotificationsEdit;

    private bool _isDataReady;
    private bool _isEditing;

    protected override void OnInitialized()
    {
        UserContactInfoSettingsService.UserContactInfoChangedEventHandler += OnContactInfoChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        _emailAddressVerified = await UserContactInfoSettingsService.GetContactInfoSettingsAsync()
            .MatchAsync(() => 
                    false,
                x => !string.IsNullOrEmpty(x.EmailAddress));

        _enableTransferNotifications = await UserNotificationSettingsService.GetNotificationSettingsAsync()
            .MatchAsync(() =>
                    false,
                x => _enableTransferNotifications = x is { EmailNotifications: true, NotifyOnTransferReceived: true });
        _enableTransferNotificationsEdit = _enableTransferNotifications;

        _isDataReady = true;
    }

    private void OnEditClicked()
    {
        _isEditing = true;
    }

    private void OnCancelClicked()
    {
        _enableTransferNotificationsEdit = _enableTransferNotifications;
        _isEditing = false;
    }

    private async Task OnSaveClickedAsync()
    {
        NotificationSettings newNotificationSettings =
            new NotificationSettings(_enableTransferNotificationsEdit, _enableTransferNotificationsEdit);
        await CrypterApiService.UserSetting.UpdateNotificationSettingsAsync(newNotificationSettings)
            .DoRightAsync(x =>
            {
                _enableTransferNotifications = x is { EmailNotifications: true, NotifyOnTransferReceived: true };
                _enableTransferNotificationsEdit = _enableTransferNotifications;
            })
            .DoLeftOrNeitherAsync(() => { _enableTransferNotificationsEdit = _enableTransferNotifications; });

        _isEditing = false;
    }

    private void OnContactInfoChanged(object? sender, UserContactInfoChangedEventArgs args)
    {
        _emailAddressVerified = args.RequestedEmailAddress;
        StateHasChanged();
    }

    public void Dispose()
    {
        UserContactInfoSettingsService.UserContactInfoChangedEventHandler -= OnContactInfoChanged;
        GC.SuppressFinalize(this);
    }
}
