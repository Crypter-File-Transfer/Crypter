using Crypter.Contracts.Requests;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class SettingsBase : ComponentBase
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
      protected string Username;
      protected string Email;
      protected string PublicAlias;
      protected bool AppearPublicly;
      protected bool AppearPubliclyPersistedValue;
      protected bool AcceptAnonymousMessages;
      protected bool AcceptAnonymousFiles;
      protected string PrivateKey;
      protected string ProfileUrl;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         IsEditing = false;

         await JSRuntime.InvokeVoidAsync("setPageTitle", "Crypter - User Search");

         if (AuthenticationService.User == null)
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         await base.OnInitializedAsync();

         await GetUserInfo();
         Loading = false;
      }

      protected async Task OnEditClicked()
      {
         await JSRuntime.InvokeVoidAsync("EditAccountDetails");
         IsEditing = true;
      }

      protected async Task OnSaveClicked()
      {
         await JSRuntime.InvokeVoidAsync("SaveAccountDetails");
         var request = new UpdateUserPrivacyRequest(PublicAlias, AppearPublicly, AcceptAnonymousMessages, AcceptAnonymousFiles);
         var (_, _) = await UserService.UpdateUserPrivacyAsync(request);
         IsEditing = false;
         AppearPubliclyPersistedValue = AppearPublicly;
      }

      protected async Task GetUserInfo()
      {
         var (_, userAccountInfo) = await UserService.GetUserSettingsAsync();
         Username = userAccountInfo.UserName;
         Email = userAccountInfo.Email;
         PublicAlias = userAccountInfo.PublicAlias;
         AppearPublicly = AppearPubliclyPersistedValue = userAccountInfo.IsPublic;
         AcceptAnonymousFiles = userAccountInfo.AllowAnonymousFiles;
         AcceptAnonymousMessages = userAccountInfo.AllowAnonymousMessages;
         PrivateKey = AuthenticationService.User.PrivateKey;
         ProfileUrl = $"{NavigationManager.BaseUri}user/profile/{Username}";
      }

      protected async Task CopyToClipboard()
      {
         await JSRuntime.InvokeVoidAsync("copyToClipboard", ProfileUrl);
      }
   }
}
