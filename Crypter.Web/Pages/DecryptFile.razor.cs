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
   public partial class DecryptFileBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected IDownloadService DownloadService { get; set; }

      [Parameter]
      public Guid ItemId { get; set; }

      protected bool Loading;
      protected bool ItemFound;

      protected string FileName;
      protected string ContentType;
      protected int Size;
      protected string Created;
      protected string Expiration;

      protected Guid SenderId;
      protected string SenderUsername;
      protected string SenderPublicAlias;
      
      protected Guid RecipientId;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         await JSRuntime.InvokeVoidAsync("setPageTitle", "Crypter - Decrypt");
         await PrepareFilePreviewAsync(); 
         await base.OnInitializedAsync();
         Loading = false;
      }

      protected async Task PrepareFilePreviewAsync()
      {
         var filePreviewRequest = new GenericPreviewRequest(ItemId);
         var withAuth = AuthenticationService.User is not null;
         var (httpStatus, response) = await DownloadService.DownloadFilePreviewAsync(filePreviewRequest, withAuth);

         ItemFound = httpStatus != HttpStatusCode.NotFound;
         if (ItemFound)
         {
            FileName = response.FileName;
            ContentType = response.ContentType;
            Created = response.CreationUTC.ToLocalTime().ToString();
            Expiration = response.ExpirationUTC.ToLocalTime().ToString();
            Size = response.Size;
            SenderId = response.SenderId;
            SenderUsername = response.SenderUsername;
            SenderPublicAlias = response.SenderPublicAlias;
            RecipientId = response.RecipientId;
         }
      }
   }
}
