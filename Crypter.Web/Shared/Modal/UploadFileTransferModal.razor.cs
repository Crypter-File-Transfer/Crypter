using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class UploadFileTransferModalBase : ComponentBase
   {
      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Parameter]
      public bool IsRecipientDefined { get; set; }

      [Parameter]
      public Guid RecipientId { get; set; }

      [Parameter]
      public string RecipientX25519PublicKey { get; set; }

      [Parameter]
      public string RecipientEd25519PublicKey { get; set; }

      [Parameter]
      public EventCallback<bool> IsRecipientDefinedChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> RecipientIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientX25519PublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientEd25519PublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback ModalClosedCallback { get; set; }

      protected bool IsSenderDefined = false;
      protected string SenderX25519PrivateKey;
      protected string SenderEd25519PrivateKey;

      protected string ModalDisplay = "none;";
      protected string ModalClass = "";
      protected bool ShowBackdrop = false;

      public void Open()
      {
         if (AuthenticationService.User is not null)
         {
            IsSenderDefined = true;
            SenderX25519PrivateKey = AuthenticationService.User.X25519PrivateKey;
            SenderEd25519PrivateKey = AuthenticationService.User.Ed25519PrivateKey;
         }

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
