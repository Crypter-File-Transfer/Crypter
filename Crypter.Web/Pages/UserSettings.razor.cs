using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
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
      IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      IUserService UserService { get; set; }

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

         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - User Search");

         if (AuthenticationService.User == null)
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
         (var _, var _) = await UserService.UpdateUserProfileInfoAsync(request);

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
         if (string.IsNullOrEmpty(CurrentPasswordForContactInfo))
         {
            // todo
            Console.WriteLine("fail");
            return;
         }
         byte[] digestedPassword = CryptoLib.UserFunctions.DigestUserCredentials(Username, CurrentPasswordForContactInfo);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var request = new UpdateContactInfoRequest(EditedEmail, digestedPasswordBase64);
         (var _, var result) = await UserService.UpdateUserContactInfoAsync(request);

         if (result.Result != UpdateContactInfoResult.Success)
         {
            // todo
            Console.WriteLine("fail");
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
         var (_, _) = await UserService.UpdateUserPrivacyAsync(request);

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
         var (_, _) = await UserService.UpdateUserNotificationAsync(request);

         EnableTransferNotifications = EditedEnableTransferNotifications;
         AreNotificationControlsEnabled = false;
         IsEditing = false;
      }

      protected async Task GetUserInfoAsync()
      {
         var (_, userAccountInfo) = await UserService.GetUserSettingsAsync();
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

         X25519PrivateKey = AuthenticationService.User.X25519PrivateKey;
         Ed25519PrivateKey = AuthenticationService.User.Ed25519PrivateKey;
         ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
      }

      protected async Task CopyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", ProfileUrl);
      }
   }
}
