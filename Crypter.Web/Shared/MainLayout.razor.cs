using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared
{
   public partial class MainLayoutBase : LayoutComponentBase
   {
      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }
   }
}
