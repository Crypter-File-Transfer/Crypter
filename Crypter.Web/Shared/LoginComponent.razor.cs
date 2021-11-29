using Crypter.Web.Helpers;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class LoginComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected ILocalStorageService LocalStorage { get; set; }

      protected Login loginInfo = new();

      protected bool LoginError = false;
      protected string LoginErrorText = "";

      protected override async Task OnInitializedAsync()
      {
         if (LocalStorage.HasItem(StoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/user/home");
         }
         await base.OnInitializedAsync();
      }

      protected async Task OnLoginClicked()
      {
         byte[] digestedPassword = CryptoLib.UserFunctions.DigestUserCredentials(loginInfo.Username, loginInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var authSuccess = await AuthenticationService.Login(loginInfo.Username, loginInfo.Password, digestedPasswordBase64);
         if (authSuccess)
         {
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "user/home";
            NavigationManager.NavigateTo(returnUrl);
            return;
         }

         LoginError = true;
         LoginErrorText = "Incorrect username or password";
      }
   }
}
