using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Crypter.Web.Shared
{
   public partial class UserReceivedItemsComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Parameter]
      public IEnumerable<UserReceivedItem> Items { get; set; }

      [Parameter]
      public EventCallback<IEnumerable<UserReceivedItem>> ItemsChanged { get; set; }
   }
}
