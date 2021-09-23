using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class NavigationBase : ComponentBase, IDisposable
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      protected Modal.UploadFileTransferModal FileTransferModal { get; set; }
      protected Modal.UploadMessageTransferModal MessageTransferModal { get; set; }

      protected override async Task OnInitializedAsync()
      {
         NavigationManager.LocationChanged += HandleLocationChanged;
         await base.OnInitializedAsync();
      }

      protected async Task OnLogoutClicked()
      {
         await AuthenticationService.Logout();
      }

      protected void HandleLocationChanged(object sender, LocationChangedEventArgs e)
      {
         StateHasChanged();
      }

      protected void OnEncryptFileClicked()
      {
         FileTransferModal.Open();
      }

      protected void OnEncryptMessageClicked()
      {
         MessageTransferModal.Open();
      }

      public void Dispose()
      {
         NavigationManager.LocationChanged -= HandleLocationChanged;
         GC.SuppressFinalize(this);
      }
   }
}
