using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserProfileBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      IUserService UserService { get; set; }

      [Parameter]
      public string Username { get; set; }

      protected Shared.Modal.UserFileUploadModal FileModal { get; set; }
      protected Shared.Modal.UserMessageUploadModal MessageModal { get; set; }

      protected bool Loading;
      protected bool ProfileFound;
      protected string PublicAlias;
      protected string ActualUsername;
      protected bool AllowsFiles;
      protected bool AllowsMessages;
      protected string UserPublicKey;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;

         await JSRuntime.InvokeVoidAsync("setPageTitle", "Crypter - User Profile");
         await base.OnInitializedAsync();

         await PrepareUserProfileAsync();
         Loading = false;
      }

      protected async Task PrepareUserProfileAsync()
      {
         var (httpStatus, response) = await UserService.GetUserPublicProfileAsync(Username);
         ProfileFound = httpStatus != HttpStatusCode.NotFound;
         if (ProfileFound)
         {
            PublicAlias = response.PublicAlias;
            ActualUsername = response.UserName;
            AllowsFiles = response.AllowsFiles;
            AllowsMessages = response.AllowsMessages;
            UserPublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(response.PublicKey));
         }
      }
   }
}
