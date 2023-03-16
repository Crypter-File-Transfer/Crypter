using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.Metrics;
using Crypter.Common.Monads;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations.Requests
{
   public class MetricsRequests : IMetricsRequests
   {
      private readonly ICrypterHttpClient _crypterHttpClient;

      public MetricsRequests(ICrypterHttpClient crypterHttpClient)
      {
         _crypterHttpClient = crypterHttpClient;
      }

      public Task<Maybe<PublicStorageMetricsResponse>> GetPublicStorageMetricsAsync()
      {
         string url = "api/metrics/storage/public";
         return _crypterHttpClient.GetMaybeAsync<PublicStorageMetricsResponse>(url);
      }
   }
}
