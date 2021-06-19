using Crypter.Contracts.Responses;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [Route("api/metrics")]
   public class MetricsController : ControllerBase
   {
      private readonly long AllocatedDiskSpace;
      private readonly IBaseItemService<MessageItem> _messageService;
      private readonly IBaseItemService<FileItem> _fileService;

      public MetricsController(IConfiguration configuration,
          IBaseItemService<MessageItem> messageService,
          IBaseItemService<FileItem> fileService
          )
      {
         AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * 1024 * 1024 * 1024;
         _messageService = messageService;
         _fileService = fileService;
      }

      [HttpGet("disk")]
      public async Task<IActionResult> GetDiskMetrics()
      {
         var sizeOfFileUploads = await _fileService.GetAggregateSizeAsync();
         var sizeOfMessageUploads = await _messageService.GetAggregateSizeAsync();
         var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
         var isFull = totalSizeOfUploads + (10 * 1024 * 1024) >= AllocatedDiskSpace;

         var responseBody = new DiskMetricsResponse(isFull, AllocatedDiskSpace.ToString(), (AllocatedDiskSpace - totalSizeOfUploads).ToString());
         return new OkObjectResult(responseBody);
      }
   }
}
