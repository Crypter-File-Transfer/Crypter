using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class TranferSuccessModalBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Parameter]
      public string UploadType { get; set; }

      [Parameter]
      public Guid ItemId { get; set; }

      [Parameter]
      public string RecipientX25519PrivateKey { get; set; }

      [Parameter]
      public EventCallback<string> UploadTypeChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> ItemIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientX25519PrivateKeyChanged { get; set; }

      [Parameter]
      public EventCallback ModalClosedCallback { get; set; }

      public string ModalDisplay = "none;";
      public string ModalClass = "";
      public bool ShowBackdrop = false;

      public void Open()
      {
         ModalDisplay = "block;";
         ModalClass = "Show";
         ShowBackdrop = true;
         StateHasChanged();
      }

      public async Task CloseAsync()
      {
         ModalDisplay = "none";
         ModalClass = "";
         ShowBackdrop = false;
         StateHasChanged();
         await ModalClosedCallback.InvokeAsync();
      }

      protected string GetDownloadLink()
      {
         return $"{NavigationManager.BaseUri}decrypt/{UploadType}/{ItemId}";
      }

      protected async Task CopyToClipboardAsync()
      {
         await JSRuntime.InvokeVoidAsync("copyToClipboard", RecipientX25519PrivateKey);
      }
   }
}
