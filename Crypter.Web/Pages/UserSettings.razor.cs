/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Settings;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserSettingsBase : ComponentBase
   {
      [Inject]
      private NavigationManager NavigationManager { get; set; }

      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Inject]
      private ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      private IUserKeysService UserKeysService { get; set; }

      protected bool Loading;
      protected bool IsEditing;
      protected bool AreProfileControlsEnabled;
      protected bool AreContactInfoControlsEnabled;
      protected bool AreNotificationControlsEnabled;
      protected bool ArePasswordControlsEnabled;
      protected bool ArePrivacyControlsEnabled;

      protected string Username;
      protected string ProfileUrl;

      // Profile
      protected string Alias;
      protected string EditedAlias;
      protected string About;
      protected string EditedAbout;

      // Contact Info
      protected string Email;
      protected string EditedEmail;
      protected string CurrentPasswordForContactInfo;
      protected bool EmailVerified;
      protected string ContactInfoEmailError;
      protected string ContactInfoPasswordError;
      protected string ContactInfoGenericError;
      protected bool UpdateContactInfoFailed;

      // Notification Settings
      protected bool EnableTransferNotifications;
      protected bool EditedEnableTransferNotifications;

      // Password
      protected string NewPassword;
      protected string CurrentPasswordForPasswordSection;

      // Privacy
      protected int Visibility;
      protected int EditedVisibility;
      protected bool AllowKeyExchangeRequests;
      protected bool EditedAllowKeyExchangeRequests;
      protected int MessageTransferPermission;
      protected int EditedMessageTransferPermission;
      protected int FileTransferPermission;
      protected int EditedFileTransferPermission;

      // Keys
      protected string X25519PrivateKey;
      protected string Ed25519PrivateKey;

      protected override async Task OnInitializedAsync()
      {
         if (!UserSessionService.LoggedIn)
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         Loading = true;
         IsEditing = false;
         AreProfileControlsEnabled = false;
         AreContactInfoControlsEnabled = false;
         AreNotificationControlsEnabled = false;
         ArePasswordControlsEnabled = false;
         ArePrivacyControlsEnabled = false;

         Username = UserSessionService.Session.Match(
            () => null,
            some => some.Username);

         ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";

         Ed25519PrivateKey = UserKeysService.Ed25519PrivateKey.Match(
            () => "",
            some => some.Value);

         X25519PrivateKey = UserKeysService.X25519PrivateKey.Match(
            () => "",
            some => some.Value);

         await GetUserInfoAsync();
         Loading = false;
      }

      protected void OnEditProfileInfoClicked()
      {
         AreProfileControlsEnabled = true;
         IsEditing = true;
      }

      protected void OnCancelProfileInfoClicked()
      {
         EditedAlias = Alias;
         EditedAbout = About;
         AreProfileControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task OnSaveProfileInfoClickedAsync()
      {
         var request = new UpdateProfileRequest(EditedAlias, EditedAbout);
         await CrypterApiService.UpdateProfileInfoAsync(request);

         Alias = EditedAlias;
         About = EditedAbout;
         AreProfileControlsEnabled = false;
         IsEditing = false;
      }

      protected void OnEditContactInfoClicked()
      {
         AreContactInfoControlsEnabled = true;
         IsEditing = true;
      }

      protected void OnCancelContactInfoClicked()
      {
         EditedEmail = Email;
         CurrentPasswordForContactInfo = "";
         AreContactInfoControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task OnSaveContactInfoClickedAsync()
      {
         ContactInfoEmailError = "";
         ContactInfoPasswordError = "";
         ContactInfoGenericError = "";

         if (!Password.TryFrom(CurrentPasswordForContactInfo, out var password))
         {
            UpdateContactInfoFailed = true;
            ContactInfoPasswordError = "Enter your current password";
            return;
         }

         Username username = Common.Primitives.Username.From(Username);

         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var request = new UpdateContactInfoRequest(EditedEmail, digestedPasswordBase64);
         var maybeUpdate = await CrypterApiService.UpdateContactInfoAsync(request);

         maybeUpdate.DoLeftOrNeither(HandleContactInfoUpdateError, () => HandleContactInfoUpdateError());
         maybeUpdate.DoRight(right =>
         {
            Email = EditedEmail;
            CurrentPasswordForContactInfo = "";
            EditedEnableTransferNotifications = false;
            EmailVerified = false;
            AreContactInfoControlsEnabled = false;
            IsEditing = false;
         });

         UpdateContactInfoFailed = !maybeUpdate.IsRight;
      }

      private void HandleContactInfoUpdateError(UpdateContactInfoError error = UpdateContactInfoError.UnknownError)
      {
         switch (error)
         {
            case UpdateContactInfoError.UnknownError:
            case UpdateContactInfoError.UserNotFound:
               ContactInfoEmailError = "An error occurred";
               break;
            case UpdateContactInfoError.EmailAddressUnavailable:
               ContactInfoEmailError = "Email address unavailable";
               break;
            case UpdateContactInfoError.InvalidEmailAddress:
               ContactInfoEmailError = "Invalid email address";
               break;
            case UpdateContactInfoError.InvalidPassword:
               ContactInfoPasswordError = "Incorrect password";
               break;
         }
      }

      protected void OnEditPasswordClicked()
      {
         ArePasswordControlsEnabled = true;
         IsEditing = true;
      }

      protected void OnCancelPasswordClicked()
      {
         NewPassword = "";
         ArePasswordControlsEnabled = false;
         IsEditing = false;
      }

      protected void OnSavePasswordClicked()
      {
         NewPassword = "";
         ArePasswordControlsEnabled = false;
         IsEditing = false;
      }

      protected void OnEditPrivacyClicked()
      {
         ArePrivacyControlsEnabled = true;
         IsEditing = true;
      }

      protected void OnCancelPrivacyClicked()
      {
         EditedAllowKeyExchangeRequests = AllowKeyExchangeRequests;
         EditedVisibility = Visibility;
         EditedMessageTransferPermission = MessageTransferPermission;
         EditedFileTransferPermission = FileTransferPermission;
         ArePrivacyControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task OnSavePrivacyClickedAsync()
      {
         var request = new UpdatePrivacySettingsRequest(EditedAllowKeyExchangeRequests, (UserVisibilityLevel)EditedVisibility, (UserItemTransferPermission)EditedMessageTransferPermission, (UserItemTransferPermission)EditedFileTransferPermission);
         await CrypterApiService.UpdateUserPrivacySettingsAsync(request);

         AllowKeyExchangeRequests = EditedAllowKeyExchangeRequests;
         Visibility = EditedVisibility;
         MessageTransferPermission = EditedMessageTransferPermission;
         FileTransferPermission = EditedFileTransferPermission;
         ArePrivacyControlsEnabled = false;
         IsEditing = false;
      }

      protected void OnEditNotificationPreferencesClicked()
      {
         AreNotificationControlsEnabled = true;
         IsEditing = true;
      }

      protected void OnCancelNotificationPreferencesClicked()
      {
         EditedEnableTransferNotifications = EnableTransferNotifications;
         AreNotificationControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task OnSaveNotificationPreferencesClickedAsync()
      {
         var request = new UpdateNotificationSettingsRequest(EditedEnableTransferNotifications, EditedEnableTransferNotifications);
         await CrypterApiService.UpdateNotificationPreferencesAsync(request);

         EnableTransferNotifications = EditedEnableTransferNotifications;
         AreNotificationControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task GetUserInfoAsync()
      {
         var maybeSettings = await CrypterApiService.GetUserSettingsAsync();
         maybeSettings.DoRight(right =>
         {
            EmailVerified = right.EmailVerified;
            EditedEmail = Email = right.EmailAddress;
            EditedAlias = Alias = right.Alias;
            EditedAbout = About = right.About;
            EditedVisibility = Visibility = (int)right.Visibility;
            EditedAllowKeyExchangeRequests = AllowKeyExchangeRequests = right.AllowKeyExchangeRequests;
            EditedMessageTransferPermission = MessageTransferPermission = (int)right.MessageTransferPermission;
            EditedFileTransferPermission = FileTransferPermission = (int)right.FileTransferPermission;
            EnableTransferNotifications = EditedEnableTransferNotifications = right.EnableTransferNotifications;
         });
      }
   }
}
