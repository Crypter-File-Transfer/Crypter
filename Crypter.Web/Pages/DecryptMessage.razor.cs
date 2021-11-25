using Crypter.Contracts.Requests;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class DecryptMessageBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected ITransferService TransferService { get; set; }

      [Parameter]
      public Guid TransferId { get; set; }

      protected bool Loading;
      protected bool ItemFound;

      protected string Subject;
      protected int Size;
      protected string Created;
      protected string Expiration;
      
      protected Guid SenderId;
      protected string SenderUsername;
      protected string SenderAlias;
      protected string X25519PublicKey;

      protected Guid RecipientId;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - Decrypt");
         await PrepareMessagePreviewAsync();
         await base.OnInitializedAsync();
         Loading = false;
      }

      protected async Task PrepareMessagePreviewAsync()
      {
         var messagePreviewRequest = new GetTransferPreviewRequest(TransferId);
         var withAuth = AuthenticationService.User is not null;
         var (httpStatus, response) = await TransferService.DownloadMessagePreviewAsync(messagePreviewRequest, withAuth);

         ItemFound = httpStatus != HttpStatusCode.NotFound;
         if (ItemFound)
         {
            Subject = string.IsNullOrEmpty(response.Subject)
               ? "{ no subject }"
               : response.Subject;
            Created = response.CreationUTC.ToLocalTime().ToString();
            Expiration = response.ExpirationUTC.ToLocalTime().ToString();
            Size = response.Size;
            SenderId = response.SenderId;
            SenderUsername = response.SenderUsername;
            SenderAlias = response.SenderAlias;
            RecipientId = response.RecipientId;
            X25519PublicKey = response.X25519PublicKey;
         }
      }
   }
}
