using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class ServerDiskSpaceComponentBase : ComponentBase
   {
      [Inject]
      protected IMetricsService MetricsService { get; set; }

      protected bool ServerHasDiskSpace = true;
      protected double ServerSpacePercentageRemaining = 100.00;

      protected override async Task OnInitializedAsync()
      {
         var (httpStatus, response) = await MetricsService.GetDiskMetricsAsync();
         if (httpStatus != HttpStatusCode.OK || response is null)
         {
            ServerHasDiskSpace = false;
         }
         else
         {
            ServerHasDiskSpace = !response.Full;
            var allocatedServerSpace = double.Parse(response.Allocated);
            var availableServerSpace = double.Parse(response.Available);
            ServerSpacePercentageRemaining = 100.00 * (availableServerSpace / allocatedServerSpace);
         }
      }
   }
}
