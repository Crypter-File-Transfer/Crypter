using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class MainLayoutBase : LayoutComponentBase
   {
      [Inject]
      private NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ILocalStorageService LocalStorage { get; set; }

      protected override async Task OnInitializedAsync()
      {
         await LocalStorage.Initialize();
         if (LocalStorage.HasItem(StoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/user/home");
         }
         await base.OnInitializedAsync();
      }
   }
}
