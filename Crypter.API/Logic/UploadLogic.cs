using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Logic
{
   public static class UploadLogic
   {
      public static async Task<UploadResult> PassesInitialValidation(MessageUploadRequest body, Guid recipientId, IBaseItemService<MessageItem> messageService, IBaseItemService<FileItem> fileService, IUserService userService, long allocatedDiskSpace, int maxUploadSize)
      {
         if (recipientId != Guid.Empty)
         {
            var recipientAllowsMessages = await userService.MessagesAllowedByUserAsync(recipientId);
            if (!recipientAllowsMessages)
            {
               return UploadResult.BlockedByUserPrivacy;
            }
         }

         var validationResult = ValidateNecessaryRequestProperties(body);
         if (validationResult != UploadResult.Success)
         {
            return validationResult;
         }

         if (!await AllocatedSpaceRemaining(messageService, fileService, allocatedDiskSpace, maxUploadSize))
         {
            return UploadResult.OutOfSpace;
         }

         return UploadResult.Success;
      }

      public static async Task<UploadResult> PassesInitialValidation(FileUploadRequest body, Guid recipientId, IBaseItemService<MessageItem> messageService, IBaseItemService<FileItem> fileService, IUserService userService, long allocatedDiskSpace, int maxUploadSize)
      {
         if (recipientId != Guid.Empty)
         {
            var recipientAllowsFiles = await userService.FilesAllowedByUserAsync(recipientId);
            if (!recipientAllowsFiles)
            {
               return UploadResult.BlockedByUserPrivacy;
            }
         }

         var validationResult = ValidateNecessaryRequestProperties(body);
         if (validationResult != UploadResult.Success)
         {
            return validationResult;
         }

         if (!await AllocatedSpaceRemaining(messageService, fileService, allocatedDiskSpace, maxUploadSize))
         {
            return UploadResult.OutOfSpace;
         }

         return UploadResult.Success;
      }

      private static async Task<bool> AllocatedSpaceRemaining(IBaseItemService<MessageItem> messageService, IBaseItemService<FileItem> fileService, long allocatedDiskSpace, int maxUploadSize)
      {
         var sizeOfFileUploads = await messageService.GetAggregateSizeAsync();
         var sizeOfMessageUploads = await fileService.GetAggregateSizeAsync();
         var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
         return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
      }

      private static UploadResult ValidateNecessaryRequestProperties(MessageUploadRequest request)
      {
         /* TODO - The web client needs to enforce this first
         if (string.IsNullOrEmpty(request.CipherTextBase64))
         {
            return UploadResult.InvalidCipherText;
         }
         */

         if (string.IsNullOrEmpty(request.ServerEncryptionKeyBase64))
         {
            return UploadResult.InvalidServerEncryptionKey;
         }

         if (string.IsNullOrEmpty(request.EncryptedSymmetricInfoBase64))
         {
            return UploadResult.InvalidEncryptedSymmetricInfo;
         }

         if (string.IsNullOrEmpty(request.SignatureBase64))
         {
            return UploadResult.InvalidSignature;
         }

         if (string.IsNullOrEmpty(request.PublicKeyBase64))
         {
            return UploadResult.InvalidPublicKey;
         }

         return UploadResult.Success;
      }

      private static UploadResult ValidateNecessaryRequestProperties(FileUploadRequest request)
      {
         if (string.IsNullOrEmpty(request.FileName))
         {
            return UploadResult.InvalidFileName;
         }

         if (string.IsNullOrEmpty(request.ContentType))
         {
            return UploadResult.InvalidContentType;
         }

         if (string.IsNullOrEmpty(request.CipherTextBase64))
         {
            return UploadResult.InvalidCipherText;
         }

         if (string.IsNullOrEmpty(request.ServerEncryptionKeyBase64))
         {
            return UploadResult.InvalidServerEncryptionKey;
         }

         if (string.IsNullOrEmpty(request.EncryptedSymmetricInfoBase64))
         {
            return UploadResult.InvalidEncryptedSymmetricInfo;
         }

         if (string.IsNullOrEmpty(request.SignatureBase64))
         {
            return UploadResult.InvalidSignature;
         }

         if (string.IsNullOrEmpty(request.PublicKeyBase64))
         {
            return UploadResult.InvalidPublicKey;
         }

         return UploadResult.Success;
      }
   }
}
