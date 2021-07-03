using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class SpinnerModalBase : ComponentBase
   {
      [Parameter]
      public string Subject { get; set; }

      [Parameter]
      public string Message { get; set; }

      [Parameter]
      public bool ShowPrimaryButton { get; set; }

      [Parameter]
      public string PrimaryButtonText { get; set; }

      [Parameter]
      public EventCallback<string> SubjectChanged { get; set; }

      [Parameter]
      public EventCallback<string> MessageChanged { get; set; }

      [Parameter]
      public EventCallback<bool> ShowPrimaryButtonChanged { get; set; }

      [Parameter]
      public EventCallback<string> PrimaryButtonTextChanged { get; set; }

      [Parameter]
      public EventCallback<bool> ModalClosedCallback { get; set; }

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
