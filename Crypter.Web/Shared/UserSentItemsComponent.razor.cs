using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Crypter.Web.Shared
{
   public partial class UserSentItemsComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Parameter]
      public IEnumerable<UserSentItem> Items { get; set; }

      [Parameter]
      public EventCallback<IEnumerable<UserSentItem>> ItemsChanged { get; set; }
   }
}
