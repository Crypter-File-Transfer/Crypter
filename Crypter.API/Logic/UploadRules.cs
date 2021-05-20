﻿using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Requests.Registered; 
using Crypter.DataAccess;
using Crypter.DataAccess.Queries;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Logic
{
    public static class UploadRules
    {
        public static async Task<bool> AllocatedSpaceRemaining(CrypterDB database, long allocatedDiskSpace, int maxUploadSize)
        {
            var sizeOfFileUploads = await new FileUploadItemQuery(database).GetSumOfSizeAsync();
            var sizeOfMessageUploads = await new TextUploadItemQuery(database).GetSumOfSizeAsync();
            var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
            return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
        }

        //this is duplicate code to isValidUploadRequest, is additional validation required for a registered user upload
        public static bool IsValidRegisteredUserUploadRequest(RegisteredUserUploadRequest request)
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
    }
}