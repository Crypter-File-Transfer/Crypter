using Crypter.API.Controllers.Methods;
using Crypter.API.Services;
using Crypter.Contracts.Requests;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [Route("api/transfer")]
   public class TransferController : ControllerBase
   {
      private readonly UploadService UploadService;
      private readonly DownloadService DownloadService;

      public TransferController(IConfiguration configuration,
          IBaseTransferService<MessageTransfer> messageService,
          IBaseTransferService<FileTransfer> fileService,
          IUserService userService,
          IUserProfileService userProfileService,
          IEmailService emailService,
          IApiValidationService apiValidationService
         )
      {
         UploadService = new UploadService(configuration, messageService, fileService, emailService, apiValidationService);
         DownloadService = new DownloadService(configuration, messageService, fileService, userService, userProfileService);
      }

      [HttpPost("message")]
      public async Task<IActionResult> MessageTransferAsync([FromBody] MessageTransferRequest request)
      {
         var senderId = ClaimsParser.ParseUserId(User);
         return await UploadService.ReceiveMessageTransferAsync(request, senderId, Guid.Empty);
      }

      [HttpPost("message/{recipientId}")]
      public async Task<IActionResult> UserMessageTransferAsync([FromBody] MessageTransferRequest request, Guid recipientId)
      {
         var senderId = ClaimsParser.ParseUserId(User);
         return await UploadService.ReceiveMessageTransferAsync(request, senderId, recipientId);
      }

      [HttpPost("file")]
      public async Task<IActionResult> FileTransferAsync([FromBody] FileTransferRequest request)
      {
         var senderId = ClaimsParser.ParseUserId(User);
         return await UploadService.ReceiveFileTransferAsync(request, senderId, Guid.Empty);
      }

      [HttpPost("file/{recipientId}")]
      public async Task<IActionResult> UserFileTransferAsync([FromBody] FileTransferRequest request, Guid recipientId)
      {
         var senderId = ClaimsParser.ParseUserId(User);
         return await UploadService.ReceiveFileTransferAsync(request, senderId, recipientId);
      }

      [HttpPost("message/preview")]
      public async Task<IActionResult> GetMessagePreviewAsync([FromBody] GetTransferPreviewRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetMessagePreviewAsync(request, requestorId);
      }

      [HttpPost("file/preview")]
      public async Task<IActionResult> GetFilePreviewAsync([FromBody] GetTransferPreviewRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetFilePreviewAsync(request, requestorId);
      }

      [HttpPost("message/ciphertext")]
      public async Task<IActionResult> GetMessageCiphertextAsync([FromBody] GetTransferCiphertextRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetMessageCiphertextAsync(request, requestorId);
      }

      [HttpPost("file/ciphertext")]
      public async Task<IActionResult> GetFileCiphertext([FromBody] GetTransferCiphertextRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetFileCiphertextAsync(request, requestorId);
      }

      [HttpPost("message/signature")]
      public async Task<IActionResult> GetMessageSignatureAsync([FromBody] GetTransferSignatureRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetMessageSignatureAsync(request, requestorId);
      }

      [HttpPost("file/signature")]
      public async Task<IActionResult> GetFileSignatureAsync([FromBody] GetTransferSignatureRequest request)
      {
         var requestorId = ClaimsParser.ParseUserId(User);
         return await DownloadService.GetFileSignatureAsync(request, requestorId);
      }
   }
}