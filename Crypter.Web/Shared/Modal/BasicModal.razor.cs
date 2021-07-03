using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class BasicModalBase : ComponentBase
   {
      [Parameter]
      public string Subject { get; set; }

      [Parameter]
      public string Message { get; set; }

      [Parameter]
      public string PrimaryButtonText { get; set; }

      [Parameter]
      public string SecondaryButtonText { get; set; }

      [Parameter]
      public bool ShowSecondaryButton { get; set; }

      [Parameter]
      public EventCallback<string> SubjectChanged { get; set; }

      [Parameter]
      public EventCallback<string> MessageChanged { get; set; }

      [Parameter]
      public EventCallback<string> PrimaryButtonTextChanged { get; set; }

      [Parameter]
      public EventCallback<string> SecondaryButtonTextChanged { get; set; }

      [Parameter]
      public EventCallback<bool> ShowSecondaryButtonChanged { get; set; }

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

      public async Task CloseAsync(bool modalClosedInTheAffirmative)
      {
         ModalDisplay = "none";
         ModalClass = "";
         ShowBackdrop = false;
         StateHasChanged();
         await ModalClosedCallback.InvokeAsync(modalClosedInTheAffirmative);
      }
   }
}
