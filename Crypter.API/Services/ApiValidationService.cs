using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public class ApiValidationService
   {
      public static async Task<bool> IsEnoughSpaceForNewTransfer(IBaseTransferService<MessageTransfer> messageTransferService, IBaseTransferService<FileTransfer> fileTransferService, long allocatedDiskSpace, int maxUploadSize)
      {
         var sizeOfFileUploads = await messageTransferService.GetAggregateSizeAsync();
         var sizeOfMessageUploads = await fileTransferService.GetAggregateSizeAsync();
         var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
         return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
      }
   }
}
