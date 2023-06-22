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

using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using Crypter.Common.Monads;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.UserSettings
{
   public partial class UserSettingsNotificationSettingsBase : ComponentBase, IDisposable
   {
      [Inject]
      protected ICrypterApiClient CrypterApiService { get; set; }

      [Inject]
      protected IUserContactInfoSettingsService UserContactInfoSettingsService { get; set; }

      [Inject]
      protected IUserNotificationSettingsService UserNotificationSettingsService { get; set; }

      protected bool EmailAddressVerified { get; set; } = false;

      protected bool EnableTransferNotifications { get; set; } = false;
      protected bool EnableTransferNotificationsEdit { get; set; } = false;

      protected bool IsDataReady { get; set; } = false;
      protected bool IsEditing { get; set; } = false;

      protected override void OnInitialized()
      {
         UserContactInfoSettingsService.UserContactInfoChangedEventHandler += OnContactInfoChanged;
      }

      protected override async Task OnParametersSetAsync()
      {
         EmailAddressVerified = await UserContactInfoSettingsService.GetContactInfoSettingsAsync()
            .MatchAsync(() =>
               false,
               x => x.EmailAddressVerified);

         EnableTransferNotifications = await UserNotificationSettingsService.GetNotificationSettingsAsync()
            .MatchAsync(() =>
               false,
               x => EnableTransferNotifications = x.EmailNotifications && x.NotifyOnTransferReceived);
         EnableTransferNotificationsEdit = EnableTransferNotifications;

         IsDataReady = true;
      }

      protected void OnEditClicked()
      {
         IsEditing = true;
      }

      protected void OnCancelClicked()
      {
         EnableTransferNotificationsEdit = EnableTransferNotifications;
         IsEditing = false;
      }

      protected async Task OnSaveClickedAsync()
      {
         NotificationSettings newNotificationSettings = new NotificationSettings(EnableTransferNotificationsEdit, EnableTransferNotificationsEdit);
         await CrypterApiService.UserSetting.UpdateNotificationSettingsAsync(newNotificationSettings)
            .DoRightAsync(x =>
            {
               EnableTransferNotifications = x.EmailNotifications && x.NotifyOnTransferReceived;
               EnableTransferNotificationsEdit = EnableTransferNotifications;
            })
            .DoLeftOrNeitherAsync(() =>
            {
               EnableTransferNotificationsEdit = EnableTransferNotifications;
            });

         IsEditing = false;
      }

      private void OnContactInfoChanged(object sender, UserContactInfoChangedEventArgs args)
      {
         EmailAddressVerified = args.EmailAddressVerified;
         StateHasChanged();
      }

      public void Dispose()
      {
         UserContactInfoSettingsService.UserContactInfoChangedEventHandler -= OnContactInfoChanged;
         GC.SuppressFinalize(this);
      }
   }
}
