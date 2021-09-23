using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class RegisterComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected IUserService UserService { get; set; }

      protected UserRegistration RegistrationInfo = new();

      protected bool RegistrationError = false;
      protected string RegistrationErrorText = "";
      protected bool RegistrationSuccess = false;

      protected override async Task OnInitializedAsync()
      {
         if (AuthenticationService.User != null)
         {
            NavigationManager.NavigateTo("/user");
         }
         await base.OnInitializedAsync();
      }

      protected async Task OnRegisterClicked()
      {
         byte[] digestedPassword = CryptoLib.UserFunctions.DigestUserCredentials(RegistrationInfo.Username, RegistrationInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var requestBody = new RegisterUserRequest(RegistrationInfo.Username, digestedPasswordBase64, RegistrationInfo.BetaKey, RegistrationInfo.Email);
         var (_, registerResponse) = await UserService.RegisterUserAsync(requestBody);

         if (registerResponse.Result != InsertUserResult.Success)
         {
            RegistrationError = true;
            RegistrationErrorText = registerResponse.ResultMessage;
         }
         else
         {
            RegistrationSuccess = true;
            StateHasChanged();
            await Task.Delay(2000);
            NavigationManager.NavigateTo("/login");
         }
      }
   }
}
