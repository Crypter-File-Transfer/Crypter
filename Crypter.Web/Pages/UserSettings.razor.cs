/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserSettingsBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      ILocalStorageService LocalStorage { get; set; }

      [Inject]
      IUserApiService UserApiService { get; set; }

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

         if (!LocalStorage.HasItem(StoredObjectType.UserSession))
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
         (var _, var _) = await UserApiService.UpdateUserProfileInfoAsync(request);

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
         (var status, var result) = await UserApiService.UpdateUserContactInfoAsync(request);

         if (status != HttpStatusCode.OK)
         {
            UpdateContactInfoFailed = true;
            ContactInfoGenericError = "An error occurred";
            return;
         }

         if (result.Result != UpdateContactInfoResult.Success)
         {
            UpdateContactInfoFailed = true;

            switch (result.Result)
            {
               case UpdateContactInfoResult.EmailUnavailable:
                  ContactInfoEmailError = "Email address unavailable";
                  break;
               case UpdateContactInfoResult.EmailInvalid:
                  ContactInfoEmailError = "Invalid email address";
                  break;
               case UpdateContactInfoResult.PasswordValidationFailed:
                  ContactInfoPasswordError = "Incorrect password";
                  break;
               default:
                  ContactInfoGenericError = "An error occurred";
                  break;
            }
            return;
         }

         Email = EditedEmail;
         CurrentPasswordForContactInfo = "";
         EditedEnableTransferNotifications = false;
         EmailVerified = false;
         AreContactInfoControlsEnabled = false;
         IsEditing = false;
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
         var request = new UpdatePrivacySettingRequest(EditedAllowKeyExchangeRequests, (UserVisibilityLevel)EditedVisibility, (UserItemTransferPermission)EditedMessageTransferPermission, (UserItemTransferPermission)EditedFileTransferPermission);
         var (_, _) = await UserApiService.UpdateUserPrivacyAsync(request);

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
         var request = new UpdateNotificationSettingRequest(EditedEnableTransferNotifications, EditedEnableTransferNotifications);
         var (_, _) = await UserApiService.UpdateUserNotificationAsync(request);

         EnableTransferNotifications = EditedEnableTransferNotifications;
         AreNotificationControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task GetUserInfoAsync()
      {
         var (status, userAccountInfo) = await UserApiService.GetUserSettingsAsync();
         if (status != HttpStatusCode.OK)
         {
            return;
         }

         Username = userAccountInfo.Username;
         EmailVerified = userAccountInfo.EmailVerified;
         EditedEmail = Email = userAccountInfo.Email;
         EditedAlias = Alias = userAccountInfo.Alias;
         EditedAbout = About = userAccountInfo.About;
         EditedVisibility = Visibility = (int)userAccountInfo.Visibility;
         EditedAllowKeyExchangeRequests = AllowKeyExchangeRequests = userAccountInfo.AllowKeyExchangeRequests;
         EditedMessageTransferPermission = MessageTransferPermission = (int)userAccountInfo.MessageTransferPermission;
         EditedFileTransferPermission = FileTransferPermission = (int)userAccountInfo.FileTransferPermission;

         EnableTransferNotifications = EditedEnableTransferNotifications = userAccountInfo.EnableTransferNotifications;

         var encryptedX25519PrivateKey = (await LocalStorage.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedX25519PrivateKey)).Key;
         var encryptedEd25519PrivateKey = (await LocalStorage.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedEd25519PrivateKey)).Key;

         X25519PrivateKey = encryptedX25519PrivateKey;
         Ed25519PrivateKey = encryptedEd25519PrivateKey;
         ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
      }

      protected async Task CopyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", ProfileUrl);
      }
   }
}
