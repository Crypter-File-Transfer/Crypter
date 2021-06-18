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
   public class DownloadService
   {
      private readonly string BaseSaveDirectory;
      private const DigestAlgorithm ItemDigestAlgorithm = DigestAlgorithm.SHA256;

      private readonly IBaseItemService<MessageItem> MessageService;
      private readonly IBaseItemService<FileItem> FileService;
      private readonly IUserService UserService;

      public DownloadService(
         IConfiguration configuration,
         IBaseItemService<MessageItem> messageService,
         IBaseItemService<FileItem> fileService,
         IUserService userService
         )
      {
         BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
         MessageService = messageService;
         FileService = fileService;
         UserService = userService;
      }

      public async Task<IActionResult> GetMessagePreviewAsync(GenericPreviewRequest request, Guid requestorId)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(
               new MessagePreviewResponse(null, 0, default, null, null, default, default, default));
         }

         var messageBelongsToSomeoneElse = possibleMessage.Recipient != Guid.Empty && possibleMessage.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new MessagePreviewResponse(null, 0, default, null, null, default, default, default));
         }

         string senderUsername = null;
         string senderPublicAlias = null;
         if (possibleMessage.Sender != Guid.Empty)
         {
            var possibleUser = await UserService.ReadAsync(possibleMessage.Sender);
            if (possibleUser != null)
            {
               senderUsername = possibleUser.UserName;
               senderPublicAlias = possibleUser.PublicAlias;
            }
         }

         return new OkObjectResult(
            new MessagePreviewResponse(possibleMessage.Subject, possibleMessage.Size, possibleMessage.Sender, senderUsername, senderPublicAlias, possibleMessage.Recipient, possibleMessage.Created, possibleMessage.Expiration));
      }

      public async Task<IActionResult> GetFilePreviewAsync(GenericPreviewRequest request, Guid requestorId)
      {
         var possibleFile = await FileService.ReadAsync(request.Id);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(
               new FilePreviewResponse(null, null, 0, default, null, null, default, default, default));
         }

         var fileBelongsToSomeoneElse = possibleFile.Recipient != Guid.Empty && possibleFile.Recipient != requestorId;
         if (fileBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new FilePreviewResponse(null, null, 0, default, null, null, default, default, default));
         }

         string senderUsername = null;
         string senderPublicAlias = null;
         if (possibleFile.Sender != Guid.Empty)
         {
            var possibleUser = await UserService.ReadAsync(possibleFile.Sender);
            if (possibleUser != null)
            {
               senderUsername = possibleUser.UserName;
               senderPublicAlias = possibleUser.PublicAlias;
            }
         }

         return new OkObjectResult(
            new FilePreviewResponse(possibleFile.FileName, possibleFile.ContentType, possibleFile.Size, possibleFile.Sender, senderUsername, senderPublicAlias, possibleFile.Recipient, possibleFile.Created, possibleFile.Expiration));
      }

      public async Task<IActionResult> GetMessageCiphertextAsync(GenericCiphertextRequest request, Guid requestorId)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(
               new GenericCiphertextResponse(DownloadCiphertextResult.NotFound, null));
         }

         var messageBelongsToSomeoneElse = possibleMessage.Recipient != Guid.Empty && possibleMessage.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new GenericCiphertextResponse(DownloadCiphertextResult.NotFound, null));
         }

         // Remove server-side encryption
         var serverDecryptionKey = Convert.FromBase64String(request.ServerDecryptionKeyBase64);
         byte[] cipherTextServer = System.IO.File.ReadAllBytes(possibleMessage.CipherTextPath);
         var symParams = CryptoLib.Common.MakeSymmetricCryptoParams(serverDecryptionKey, possibleMessage.ServerIV);
         byte[] cipherTextClient = CryptoLib.Common.UndoSymmetricEncryption(cipherTextServer, symParams);

         // Compare digests AFTER removing server-side encryption
         var digestsMatch = CryptoLib.Common.VerifyPlaintextAgainstKnownDigest(cipherTextClient, possibleMessage.ServerDigest, ItemDigestAlgorithm);
         if (!digestsMatch)
         {
            return new BadRequestObjectResult(
                new GenericCiphertextResponse(DownloadCiphertextResult.ServerDecryptionFailed, null));
         }

         await MessageService.DeleteAsync(request.Id);
         FileCleanup DownloadDir = new FileCleanup(request.Id, BaseSaveDirectory);
         DownloadDir.CleanDirectory(false);

         return new OkObjectResult(
             new GenericCiphertextResponse(DownloadCiphertextResult.Success, Convert.ToBase64String(cipherTextClient)));
      }

      public async Task<IActionResult> GetFileCiphertextAsync(GenericCiphertextRequest request, Guid requestorId)
      {
         var possibleFile = await FileService.ReadAsync(request.Id);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(
               new GenericCiphertextResponse(DownloadCiphertextResult.NotFound, null));
         }

         var messageBelongsToSomeoneElse = possibleFile.Recipient != Guid.Empty && possibleFile.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new GenericCiphertextResponse(DownloadCiphertextResult.NotFound, null));
         }

         // Remove server-side encryption
         var serverDecryptionKey = Convert.FromBase64String(request.ServerDecryptionKeyBase64);
         byte[] cipherTextServer = System.IO.File.ReadAllBytes(possibleFile.CipherTextPath);
         var symParams = CryptoLib.Common.MakeSymmetricCryptoParams(serverDecryptionKey, possibleFile.ServerIV);
         byte[] cipherTextClient = CryptoLib.Common.UndoSymmetricEncryption(cipherTextServer, symParams);

         // Compare digests AFTER removing server-side encryption
         var digestsMatch = CryptoLib.Common.VerifyPlaintextAgainstKnownDigest(cipherTextClient, possibleFile.ServerDigest, ItemDigestAlgorithm);
         if (!digestsMatch)
         {
            return new BadRequestObjectResult(
                new GenericCiphertextResponse(DownloadCiphertextResult.ServerDecryptionFailed, null));
         }

         await FileService.DeleteAsync(request.Id);
         FileCleanup DownloadDir = new FileCleanup(request.Id, BaseSaveDirectory);
         DownloadDir.CleanDirectory(true);

         return new OkObjectResult(
             new GenericCiphertextResponse(DownloadCiphertextResult.Success, Convert.ToBase64String(cipherTextClient)));
      }

      public async Task<IActionResult> GetMessageSignatureAsync(GenericSignatureRequest request, Guid requestorId)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(
               new GenericSignatureResponse(DownloadSignatureResult.NotFound, null, null, null));
         }

         var messageBelongsToSomeoneElse = possibleMessage.Recipient != Guid.Empty && possibleMessage.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new GenericSignatureResponse(DownloadSignatureResult.NotFound, null, null, null));
         }

         return new OkObjectResult(
            new GenericSignatureResponse(DownloadSignatureResult.Success, possibleMessage.Signature, possibleMessage.PublicKey, possibleMessage.SymmetricInfo));
      }

      public async Task<IActionResult> GetFileSignatureAsync(GenericSignatureRequest request, Guid requestorId)
      {
         var possibleFile = await FileService.ReadAsync(request.Id);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(
               new GenericSignatureResponse(DownloadSignatureResult.NotFound, null, null, null));
         }

         var messageBelongsToSomeoneElse = possibleFile.Recipient != Guid.Empty && possibleFile.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(
               new GenericSignatureResponse(DownloadSignatureResult.NotFound, null, null, null));
         }

         return new OkObjectResult(
            new GenericSignatureResponse(DownloadSignatureResult.Success, possibleFile.Signature, possibleFile.PublicKey, possibleFile.SymmetricInfo));
      }
   }
}
