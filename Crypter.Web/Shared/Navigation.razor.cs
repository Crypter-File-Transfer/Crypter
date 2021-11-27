using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class NavigationBase : ComponentBase, IDisposable
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ILocalStorageService LocalStorageService { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      protected Modal.UploadFileTransferModal FileTransferModal { get; set; }
      protected Modal.UploadMessageTransferModal MessageTransferModal { get; set; }

      protected override async Task OnInitializedAsync()
      {
         NavigationManager.LocationChanged += HandleLocationChanged;
         await base.OnInitializedAsync();
      }

      protected void OnLogoutClicked()
      {
         AuthenticationService.Logout();
      }

      protected void HandleLocationChanged(object sender, LocationChangedEventArgs e)
      {
         InvokeAsync(async () =>
         {
            await CollapseNavigationMenuAsync();
         });
      }

      protected async Task OnEncryptFileClicked()
      {
         await FileTransferModal.Open();
      }

      protected async Task OnEncryptMessageClicked()
      {
         await MessageTransferModal.Open();
      }

      public async Task CollapseNavigationMenuAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.CollapseNavBar");
         StateHasChanged();
      }

      public void Dispose()
      {
         NavigationManager.LocationChanged -= HandleLocationChanged;
         GC.SuppressFinalize(this);
      }
   }
}
