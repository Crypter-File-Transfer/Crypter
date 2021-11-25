using Crypter.Contracts.Enum;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class UserHomeBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      IUserService UserService { get; set; }

      protected IEnumerable<UserSentItem> Sent;
      protected IEnumerable<UserReceivedItem> Received;

      protected override async Task OnInitializedAsync()
      {
         if (AuthenticationService.User == null)
         {
            NavigationManager.NavigateTo("/");
         }

         await JSRuntime.InvokeVoidAsync("Crypter.SetPageTitle", "Crypter - User Home");
         await base.OnInitializedAsync();

         Sent = await GetUserSentItems();
         Received = await GetUserReceivedItems();
      }

      protected async Task<IEnumerable<UserSentItem>> GetUserSentItems()
      {
         var (_, sentMessagesResponse) = await UserService.GetUserSentMessagesAsync();
         var (_, sentFilesresponse) = await UserService.GetUserSentFilesAsync();

         return sentMessagesResponse.Messages
            .Select(x => new UserSentItem
            {
               Id = x.Id,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               RecipientId = x.RecipientId,
               RecipientUsername = x.RecipientUsername,
               RecipientAlias = x.RecipientAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(sentFilesresponse.Files
               .Select(x => new UserSentItem
               {
                  Id = x.Id,
                  Name = x.FileName,
                  RecipientId = x.RecipientId,
                  RecipientUsername = x.RecipientUsername,
                  RecipientAlias = x.RecipientAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }

      protected async Task<IEnumerable<UserReceivedItem>> GetUserReceivedItems()
      {
         var (_, receivedMessagesResponse) = await UserService.GetUserReceivedMessagesAsync();
         var (_, receivedFilesresponse) = await UserService.GetUserReceivedFilesAsync();

         return receivedMessagesResponse.Messages
            .Select(x => new UserReceivedItem
            {
               Id = x.Id,
               Name = string.IsNullOrEmpty(x.Subject) ? "{no subject}" : x.Subject,
               SenderId = x.SenderId,
               SenderUsername = x.SenderUsername,
               SenderAlias = x.SenderAlias,
               ItemType = TransferItemType.Message,
               ExpirationUTC = x.ExpirationUTC
            })
            .Concat(receivedFilesresponse.Files
               .Select(x => new UserReceivedItem
               {
                  Id = x.Id,
                  Name = x.FileName,
                  SenderId = x.SenderId,
                  SenderUsername = x.SenderUsername,
                  SenderAlias = x.SenderAlias,
                  ItemType = TransferItemType.File,
                  ExpirationUTC = x.ExpirationUTC
               }))
            .OrderBy(x => x.ExpirationUTC);
      }
   }
}
