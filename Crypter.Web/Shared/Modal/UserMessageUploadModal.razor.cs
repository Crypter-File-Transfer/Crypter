using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class UserMessageUploadModalBase : ComponentBase
   {
      [Parameter]
      public string RecipientUsername { get; set; }

      [Parameter]
      public string RecipientPublicKey { get; set; }

      [Parameter]
      public EventCallback<string> RecipientUsernameChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientPublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback ModalClosedCallback { get; set; }

      protected string ModalDisplay = "none;";
      protected string ModalClass = "";
      protected bool ShowBackdrop = false;

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


   }
}
