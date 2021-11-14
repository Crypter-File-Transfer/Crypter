using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.CryptoLib.Enums;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Crypter.Core.Services;
using Hangfire;

namespace Crypter.API.Services
{
   public class UploadService
   {
      private readonly long AllocatedDiskSpace;
      private readonly int MaxUploadSize;
      private const SHAFunction ItemDigestAlgorithm = SHAFunction.SHA256;

      private readonly IBaseTransferService<MessageTransfer> MessageTransferService;
      private readonly IBaseTransferService<FileTransfer> FileTransferService;

      private readonly IEmailService EmailService;

      private readonly ITransferItemStorageService MessageTransferItemStorageService;
      private readonly ITransferItemStorageService FileTransferItemStorageService;

      public UploadService(
         IConfiguration configuration,
         IBaseTransferService<MessageTransfer> messageTransferService,
         IBaseTransferService<FileTransfer> fileTransferService,
         IEmailService emailService
         )
      {
         AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
         MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
         MessageTransferService = messageTransferService;
         FileTransferService = fileTransferService;
         EmailService = emailService;

         MessageTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.Message);
         FileTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.File);
      }

      private async Task<(UploadResult Result, BaseTransfer GenericTransferData, byte[] ServerEncryptedCipherText)> ReceiveTransferAsync(ITransferRequest request, Guid senderId, Guid recipientId)
      {
         var serverHasSpaceRemaining = await ApiValidationService.IsEnoughSpaceForNewTransfer(MessageTransferService, FileTransferService, AllocatedDiskSpace, MaxUploadSize);
         if (!serverHasSpaceRemaining)
         {
            return (UploadResult.OutOfSpace, null, null);
         }

         // Digest the ciphertext BEFORE applying server-side encryption
         byte[] originalCiphertextBytes;
         try
         {
            originalCiphertextBytes = Convert.FromBase64String(request.CipherTextBase64);
         }
         catch (Exception)
         {
            return (UploadResult.InvalidCipherText, null, null);
         }
         var digestor = new CryptoLib.Crypto.SHA(ItemDigestAlgorithm);
         digestor.BlockUpdate(originalCiphertextBytes);
         var serverDigest = digestor.GetDigest();

         // Apply server-side encryption
         byte[] hashedSymmetricEncryptionKey;
         try
         {
            hashedSymmetricEncryptionKey = Convert.FromBase64String(request.ServerEncryptionKeyBase64);
         }
         catch (Exception)
         {
            return (UploadResult.InvalidServerEncryptionKey, null, null);
         }

         if (hashedSymmetricEncryptionKey.Length != 32)
         {
            return (UploadResult.InvalidServerEncryptionKey, null, null);
         }

         var serverEncryptionIV = CryptoLib.Crypto.AES.GenerateIV();
         var encrypter = new CryptoLib.Crypto.AES();
         encrypter.Initialize(hashedSymmetricEncryptionKey, serverEncryptionIV, true);
         var ciphertextServerEncrypted = encrypter.ProcessFinal(originalCiphertextBytes);

         Guid itemId = Guid.NewGuid();
         var itemCreated = DateTime.UtcNow;
         var itemExpires = itemCreated.AddDays(1);

         var returnItem = new BaseTransfer(itemId, senderId, recipientId, originalCiphertextBytes.Length, request.ClientEncryptionIVBase64, request.SignatureBase64, request.X25519PublicKeyBase64, request.Ed25519PublicKeyBase64, serverEncryptionIV, serverDigest, itemCreated, itemExpires);
         return (UploadResult.Success, returnItem, ciphertextServerEncrypted);
      }

      public async Task<IActionResult> ReceiveMessageTransferAsync(MessageTransferRequest request, Guid senderId, Guid recipientId)
      {
         (var receiveResult, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId);

         if (receiveResult != UploadResult.Success)
         {
            return new BadRequestObjectResult(
               new TransferUploadResponse(receiveResult, default, default));
         }

         var saveResult = await MessageTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted);
         if (!saveResult)
         {
            return new BadRequestObjectResult(
                new TransferUploadResponse(UploadResult.Unknown, default, default));
         }

         var messageItem = new MessageTransfer(
               genericTransferData.Id,
               senderId,
               recipientId,
               request.Subject,
               genericTransferData.Size,
               genericTransferData.ClientIV,
               genericTransferData.Signature,
               genericTransferData.X25519PublicKey,
               genericTransferData.Ed25519PublicKey,
               genericTransferData.ServerIV,
               genericTransferData.ServerDigest,
               genericTransferData.Created,
               genericTransferData.Expiration);

         await MessageTransferService.InsertAsync(messageItem);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => EmailService.HangfireSendTransferNotificationAsync(TransferItemType.Message, messageItem.Id));
         }

         return new OkObjectResult(
             new TransferUploadResponse(UploadResult.Success, genericTransferData.Id, genericTransferData.Expiration));
      }

      public async Task<IActionResult> ReceiveFileTransferAsync(FileTransferRequest request, Guid senderId, Guid recipientId)
      {
         (var receiveResult, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId);

         if (receiveResult != UploadResult.Success)
         {
            return new BadRequestObjectResult(
               new TransferUploadResponse(receiveResult, default, default));
         }

         var saveResult = await FileTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted);
         if (!saveResult)
         {
            return new BadRequestObjectResult(
                new TransferUploadResponse(UploadResult.Unknown, default, default));
         }

         var fileItem = new FileTransfer(
               genericTransferData.Id,
               senderId,
               recipientId,
               request.FileName,
               request.ContentType,
               genericTransferData.Size,
               genericTransferData.ClientIV,
               genericTransferData.Signature,
               genericTransferData.X25519PublicKey,
               genericTransferData.Ed25519PublicKey,
               genericTransferData.ServerIV,
               genericTransferData.ServerDigest,
               genericTransferData.Created,
               genericTransferData.Expiration);

         await FileTransferService.InsertAsync(fileItem);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => EmailService.HangfireSendTransferNotificationAsync(TransferItemType.File, fileItem.Id));
         }

         return new OkObjectResult(
             new TransferUploadResponse(UploadResult.Success, genericTransferData.Id, genericTransferData.Expiration));
      }
   }
}
