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
      IUserService UserService { get; set; }

      [Inject]
      IAuthenticationService AuthenticationService { get; set; }

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

      protected bool IsVisitorAuthenticated = false;
      protected Guid VisitorId;
      protected string VisitorX25519PrivateKey;
      protected string VisitorEd25519PrivateKey;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;

         await JSRuntime.InvokeVoidAsync("setPageTitle", "Crypter - User Profile");
         await base.OnInitializedAsync();

         await PrepareUserProfileAsync();

         if (AuthenticationService.User is not null)
         {
            IsVisitorAuthenticated = true;
            VisitorId = AuthenticationService.User.Id;
            VisitorX25519PrivateKey = AuthenticationService.User.X25519PrivateKey;
            VisitorEd25519PrivateKey = AuthenticationService.User.Ed25519PrivateKey;
         }

         Loading = false;
      }

      protected async Task PrepareUserProfileAsync()
      {
         var requestWithAuthentication = AuthenticationService.User is not null;
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
