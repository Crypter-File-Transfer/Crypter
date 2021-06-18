using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class DiskMetricsResponse
   {
      public bool Full { get; set; }
      public string Allocated { get; set; }
      public string Available { get; set; }

      [JsonConstructor]
      public DiskMetricsResponse(bool full, string allocated, string available)
      {
         Full = full;
         Allocated = allocated;
         Available = available;
      }
   }
}
