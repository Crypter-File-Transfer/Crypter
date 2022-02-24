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
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.User.UpdateContactInfo;
using Crypter.Contracts.Features.User.UpdateNotificationSettings;
using Crypter.Contracts.Features.User.UpdatePrivacySettings;
using Crypter.Contracts.Features.User.UpdateProfile;
using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserSettingsBase : ComponentBase
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> BrowserStorageService { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

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
         Loading = true;
         IsEditing = false;
         AreProfileControlsEnabled = false;
         AreContactInfoControlsEnabled = false;
         AreNotificationControlsEnabled = false;
         ArePasswordControlsEnabled = false;
         ArePrivacyControlsEnabled = false;

         if (!BrowserStorageService.HasItem(BrowserStoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         await base.OnInitializedAsync();

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
         await CrypterApiService.UpdateUserProfileInfoAsync(request);

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

         if (string.IsNullOrEmpty(CurrentPasswordForContactInfo))
         {
            UpdateContactInfoFailed = true;
            ContactInfoPasswordError = "Enter your current password";
            return;
         }

         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(Username, CurrentPasswordForContactInfo);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var request = new UpdateContactInfoRequest(EditedEmail, digestedPasswordBase64);
         var maybeUpdate = await CrypterApiService.UpdateUserContactInfoAsync(request);
         UpdateContactInfoFailed = maybeUpdate.Match(
            left =>
            {
               switch (left)
               {
                  case UpdateContactInfoError.UserNotFound:
                  case UpdateContactInfoError.ErrorResettingNotificationPreferences:
                     ContactInfoGenericError = "This shouldn't happen";
                     break;
                  case UpdateContactInfoError.EmailUnavailable:
                     ContactInfoEmailError = "Email address unavailable";
                     break;
                  case UpdateContactInfoError.EmailInvalid:
                     ContactInfoEmailError = "Invalid email address";
                     break;
                  case UpdateContactInfoError.PasswordValidationFailed:
                     ContactInfoPasswordError = "Incorrect password";
                     break;
                  case UpdateContactInfoError.UnknownError:
                  default:
                     ContactInfoGenericError = "An error occurred";
                     break;
               }
               return true;
            },
            right =>
            {
               Email = EditedEmail;
               CurrentPasswordForContactInfo = "";
               EditedEnableTransferNotifications = false;
               EmailVerified = false;
               AreContactInfoControlsEnabled = false;
               IsEditing = false;
               return false;
            });
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
         await CrypterApiService.UpdateUserPrivacyAsync(request);

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
         await CrypterApiService.UpdateUserNotificationAsync(request);

         EnableTransferNotifications = EditedEnableTransferNotifications;
         AreNotificationControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task GetUserInfoAsync()
      {
         var maybeSettings = await CrypterApiService.GetUserSettingsAsync();
         await maybeSettings.DoRightAsync(async right =>
         {
            Username = right.Username;
            EmailVerified = right.EmailVerified;
            EditedEmail = Email = right.Email;
            EditedAlias = Alias = right.Alias;
            EditedAbout = About = right.About;
            EditedVisibility = Visibility = (int)right.Visibility;
            EditedAllowKeyExchangeRequests = AllowKeyExchangeRequests = right.AllowKeyExchangeRequests;
            EditedMessageTransferPermission = MessageTransferPermission = (int)right.MessageTransferPermission;
            EditedFileTransferPermission = FileTransferPermission = (int)right.FileTransferPermission;

            EnableTransferNotifications = EditedEnableTransferNotifications = right.EnableTransferNotifications;

            var encryptedX25519PrivateKey = (await BrowserStorageService.GetItemAsync<EncryptedPrivateKey>(BrowserStoredObjectType.EncryptedX25519PrivateKey)).Key;
            var encryptedEd25519PrivateKey = (await BrowserStorageService.GetItemAsync<EncryptedPrivateKey>(BrowserStoredObjectType.EncryptedEd25519PrivateKey)).Key;

            X25519PrivateKey = encryptedX25519PrivateKey;
            Ed25519PrivateKey = encryptedEd25519PrivateKey;
            ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
         });
      }
   }
}
