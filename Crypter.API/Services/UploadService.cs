using Crypter.API.Logic;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.CryptoLib.Enums;
using Crypter.DataAccess.FileSystem;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public class UploadService
   {
      private readonly string BaseSaveDirectory;
      private readonly long AllocatedDiskSpace;
      private readonly int MaxUploadSize;
      private const DigestAlgorithm ItemDigestAlgorithm = DigestAlgorithm.SHA256;

      private readonly IBaseItemService<MessageItem> MessageService;
      private readonly IBaseItemService<FileItem> FileService;
      private readonly IUserService UserService;

      public UploadService(
         IConfiguration configuration,
         IBaseItemService<MessageItem> messageService,
         IBaseItemService<FileItem> fileService,
         IUserService userService
         )
      {
         BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
         AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
         MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
         MessageService = messageService;
         FileService = fileService;
         UserService = userService;
      }

      public async Task<IActionResult> UploadMessageAsync(MessageUploadRequest request, Guid senderId)
      {
         var recipientId = string.IsNullOrEmpty(request.RecipientUsername)
            ? Guid.Empty
            : await UserService.UserIdFromUsernameAsync(request.RecipientUsername.ToLower());

         var initialValidationResult = await UploadLogic.PassesInitialValidation(request, recipientId, MessageService, FileService, UserService, AllocatedDiskSpace, MaxUploadSize);
         if (initialValidationResult != UploadResult.Success)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(initialValidationResult, default, default));
         }

         // Digest the ciphertext BEFORE applying server-side encryption
         byte[] originalCiphertextBytes;
         try
         {
            originalCiphertextBytes = Convert.FromBase64String(request.CipherTextBase64);
         }
         catch (Exception)
         {
            return new BadRequestObjectResult(
                new GenericUploadResponse(UploadResult.InvalidCipherText, default, default));
         }
         var serverDigest = CryptoLib.Common.GetDigest(originalCiphertextBytes, ItemDigestAlgorithm);

         // Apply server-side encryption
         byte[] hashedSymmetricEncryptionKey;
         try
         {
            hashedSymmetricEncryptionKey = Convert.FromBase64String(request.ServerEncryptionKeyBase64);
         }
         catch (Exception)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(UploadResult.InvalidServerEncryptionKey, default, default));
         }

         if (hashedSymmetricEncryptionKey.Length != 32)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(UploadResult.InvalidServerEncryptionKey, default, default));
         }

         var iv = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
         var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(hashedSymmetricEncryptionKey, iv);
         var cipherTextBytesServerEncrypted = CryptoLib.Common.DoSymmetricEncryption(originalCiphertextBytes, symmetricParams);

         Guid itemGuid = Guid.NewGuid();
         var itemCreated = DateTime.UtcNow;
         var itemExpires = itemCreated.AddDays(1);
         var filepaths = new CreateFilePaths(BaseSaveDirectory);

         var saveResult = filepaths.SaveToFileSystem(itemGuid, cipherTextBytesServerEncrypted, false);
         if (!saveResult)
         {
            return new BadRequestObjectResult(
                new GenericUploadResponse(UploadResult.Unknown, default, default));
         }

         var messageItem = new MessageItem(
               itemGuid,
               senderId,
               recipientId,
               request.Subject,
               filepaths.FileSizeBytes(filepaths.ActualPathString),
               filepaths.ActualPathString,
               request.SignatureBase64,
               request.EncryptedSymmetricInfoBase64,
               request.PublicKeyBase64,
               iv,
               serverDigest,
               itemCreated,
               itemExpires);

         await MessageService.InsertAsync(messageItem);

         return new OkObjectResult(
             new GenericUploadResponse(UploadResult.Success, itemGuid, itemExpires));
      }

      public async Task<IActionResult> UploadFileAsync(FileUploadRequest request, Guid senderId)
      {
         var recipientId = string.IsNullOrEmpty(request.RecipientUsername)
            ? Guid.Empty
            : await UserService.UserIdFromUsernameAsync(request.RecipientUsername.ToLower());

         var initialValidationResult = await UploadLogic.PassesInitialValidation(request, recipientId, MessageService, FileService, UserService, AllocatedDiskSpace, MaxUploadSize);
         if (initialValidationResult != UploadResult.Success)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(initialValidationResult, default, default));
         }

         // Digest the ciphertext BEFORE applying server-side encryption
         byte[] originalCiphertextBytes;
         try
         {
            originalCiphertextBytes = Convert.FromBase64String(request.CipherTextBase64);
         }
         catch (Exception)
         {
            return new BadRequestObjectResult(
                new GenericUploadResponse(UploadResult.InvalidCipherText, default, default));
         }
         var serverDigest = CryptoLib.Common.GetDigest(originalCiphertextBytes, ItemDigestAlgorithm);

         // Apply server-side encryption
         byte[] hashedSymmetricEncryptionKey;
         try
         {
            hashedSymmetricEncryptionKey = Convert.FromBase64String(request.ServerEncryptionKeyBase64);
         }
         catch (Exception)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(UploadResult.InvalidServerEncryptionKey, default, default));
         }

         if (hashedSymmetricEncryptionKey.Length != 32)
         {
            return new BadRequestObjectResult(
               new GenericUploadResponse(UploadResult.InvalidServerEncryptionKey, default, default));
         }

         var iv = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
         var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(hashedSymmetricEncryptionKey, iv);
         var cipherTextBytesServerEncrypted = CryptoLib.Common.DoSymmetricEncryption(originalCiphertextBytes, symmetricParams);

         Guid itemGuid = Guid.NewGuid();
         var itemCreated = DateTime.UtcNow;
         var itemExpires = itemCreated.AddDays(1);
         var filepaths = new CreateFilePaths(BaseSaveDirectory);

         var saveResult = filepaths.SaveToFileSystem(itemGuid, cipherTextBytesServerEncrypted, true);
         if (!saveResult)
         {
            return new BadRequestObjectResult(
                new GenericUploadResponse(UploadResult.Unknown, default, default));
         }

         var fileItem = new FileItem(
               itemGuid,
               senderId,
               recipientId,
               request.FileName,
               request.ContentType,
               filepaths.FileSizeBytes(filepaths.ActualPathString),
               filepaths.ActualPathString,
               request.SignatureBase64,
               request.EncryptedSymmetricInfoBase64,
               request.PublicKeyBase64,
               iv,
               serverDigest,
               itemCreated,
               itemExpires);

         await FileService.InsertAsync(fileItem);

         return new OkObjectResult(
             new GenericUploadResponse(UploadResult.Success, itemGuid, itemExpires));
      }
   }
}
