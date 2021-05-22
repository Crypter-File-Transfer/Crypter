using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Logic
{
    public static class UploadRules
    {
        public static async Task<bool> AllocatedSpaceRemaining(IBaseItemService<MessageItem> messageService, IBaseItemService<FileItem> fileService, long allocatedDiskSpace, int maxUploadSize)
        {
            var sizeOfFileUploads = await messageService.GetAggregateSizeAsync();
            var sizeOfMessageUploads = await fileService.GetAggregateSizeAsync();
            var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
            return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
        }

        public static bool IsValidUploadRequest(string cipherText, string serverEncryptionKey)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return false;
            }

            if (string.IsNullOrEmpty(serverEncryptionKey))
            {
                return false;
            }

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                byte[] key = Convert.FromBase64String(serverEncryptionKey);
                if (Buffer.ByteLength(key) != 32)
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }
    }
}
