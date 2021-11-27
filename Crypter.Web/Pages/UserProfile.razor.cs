using Crypter.Web.Services;
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
      IUserApiService UserService { get; set; }

      [Inject]
      ILocalStorageService LocalStorage { get; set; }

      [Parameter]
      public string Username { get; set; }

      protected Shared.Modal.UploadFileTransferModal FileModal { get; set; }
      protected Shared.Modal.UploadMessageTransferModal MessageModal { get; set; }

      protected bool Loading;
      protected bool ProfileFound;
      protected Guid UserId;
      protected string Alias;
      protected string About;
      protected string ActualUsername;
      protected bool AllowsFiles;
      protected bool AllowsMessages;
      protected string UserX25519PublicKey;
      protected string UserEd25519PublicKey;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;

         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - User Profile");
         await base.OnInitializedAsync();

         await PrepareUserProfileAsync();

         Loading = false;
      }

      protected async Task PrepareUserProfileAsync()
      {
         var requestWithAuthentication = LocalStorage.HasItem(StoredObjectType.UserSession);
         var (httpStatus, response) = await UserService.GetUserPublicProfileAsync(Username, requestWithAuthentication);
         ProfileFound = httpStatus != HttpStatusCode.NotFound;
         if (ProfileFound)
         {
            UserId = response.Id;
            Alias = response.Alias;
            About = response.About;
            ActualUsername = response.Username;
            AllowsFiles = response.ReceivesFiles;
            AllowsMessages = response.ReceivesMessages;
            UserX25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(response.PublicDHKey));
            UserEd25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(response.PublicDSAKey));
         }
      }
   }
}
