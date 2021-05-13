using Crypter.Contracts.Responses.Metrics;
using Crypter.DataAccess;
using Crypter.DataAccess.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
    [Route("api/metrics")]
    [Produces("application/json")]
    //[ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly CrypterDB Db;
        private readonly long AllocatedDiskSpace;

        public MetricsController(CrypterDB db, IConfiguration configuration)
        {
            Db = db;
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * 1024 * 1024 * 1024;
        }

        // GET: crypter.dev/api/metrics/disk
        [HttpGet("disk")]
        public async Task<IActionResult> GetDiskMetrics()
        {
            Db.Connection.Open();
            var sizeOfFileUploads = await new FileUploadItemQuery(Db).GetSumOfSizeAsync();
            var sizeOfMessageUploads = await new TextUploadItemQuery(Db).GetSumOfSizeAsync();
            var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
            var isFull = totalSizeOfUploads + (10 * 1024 * 1024) >= AllocatedDiskSpace;

            var responseBody = new DiskMetricsResponse(isFull, AllocatedDiskSpace.ToString(), (AllocatedDiskSpace - totalSizeOfUploads).ToString());
            return new JsonResult(responseBody);
        }
    }
}
