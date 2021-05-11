using Crypter.Contracts.Requests.Anonymous;
using Crypter.DataAccess;
using Crypter.DataAccess.Queries;
using System;

namespace Crypter.API.Services
{
    public static class UploadRules
    {
        public static bool AllocatedSpaceRemaining(CrypterDB database, long allocatedDiskSpace, int maxUploadSize)
        {
            var sizeOfFileUploads = new FileUploadItemQuery(database).GetSumOfSize();
            var sizeOfMessageUploads = new TextUploadItemQuery(database).GetSumOfSize();
            var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
            return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
        }

        public static bool IsValidUploadRequest(AnonymousUploadRequest request)
        {
            if (string.IsNullOrEmpty(request.CipherText))
            {
                return false;
            }

            if (string.IsNullOrEmpty(request.ServerEncryptionKey))
            {
                return false;
            }

            try
            {
                byte[] cipherText = Convert.FromBase64String(request.CipherText);
                byte[] key = Convert.FromBase64String(request.ServerEncryptionKey);
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

        public static bool IsValidPassword(string userInput)
        {
            if(string.IsNullOrWhiteSpace(userInput))
            {
                return false;
            }
            //validate password requirements
            //meets min chars
            //meets special char
            //meets upper/lower reqs
            // does not contain email, publicalias, username

            return true; 
        }
    }
}
