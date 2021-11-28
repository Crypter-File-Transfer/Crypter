using Crypter.Contracts.Requests;
using Crypter.Web.Models;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class VerifyBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      IUserApiService UserService { get; set; }

      protected EmailVerificationParams EmailVerificationParams = new();

      protected bool EmailVerificationInProgress = true;
      protected bool EmailVerificationSuccess = false;

      protected override async Task OnInitializedAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - Verify");
         ParseVerificationParamsFromUri();
         await VerifyEmailAddressAsync();

         await base.OnInitializedAsync();
      }

      protected void ParseVerificationParamsFromUri()
      {
         var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("code", out var code))
         {
            EmailVerificationParams.Code = code.First();
         }

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("signature", out var signature))
         {
            EmailVerificationParams.Signature = signature.First();
         }
      }

      protected async Task VerifyEmailAddressAsync()
      {
         (var _, var response) = await UserService.VerifyUserEmailAddressAsync(
            new VerifyUserEmailAddressRequest(EmailVerificationParams.Code, EmailVerificationParams.Signature));
         EmailVerificationSuccess = response.Success;
         EmailVerificationInProgress = false;
      }
   }
}
