using Crypter.Contracts.Responses;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [Route("api/metrics")]
   public class MetricsController : ControllerBase
   {
      private readonly long AllocatedDiskSpace;
      private readonly IBaseTransferService<MessageTransfer> _messageService;
      private readonly IBaseTransferService<FileTransfer> _fileService;

      public MetricsController(IConfiguration configuration,
          IBaseTransferService<MessageTransfer> messageService,
          IBaseTransferService<FileTransfer> fileService
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
