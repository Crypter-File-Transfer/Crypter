using Crypter.API.Services;
using Crypter.Contracts.Requests;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [Route("api/download")]
   public class DownloadController : ControllerBase
   {
      private readonly DownloadService DownloadService;

      public DownloadController(IConfiguration configuration,
         IBaseItemService<MessageItem> messageService,
         IBaseItemService<FileItem> fileService,
         IUserService userService
         )
      {
         DownloadService = new DownloadService(configuration, messageService, fileService, userService);
      }

      [HttpPost("message/preview/anon")]
      public async Task<IActionResult> GetAnonymousMessagePreviewAsync([FromBody] GenericPreviewRequest request)
      {
         return await DownloadService.GetMessagePreviewAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("message/preview/auth")]
      public async Task<IActionResult> GetAuthenticatedMessagePreviewAsync([FromBody] GenericPreviewRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetMessagePreviewAsync(request, requestorId);
      }

      [HttpPost("file/preview/anon")]
      public async Task<IActionResult> GetAnonymousFilePreviewAsync([FromBody] GenericPreviewRequest request)
      {
         return await DownloadService.GetFilePreviewAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("file/preview/auth")]
      public async Task<IActionResult> GetAuthenticatedFilePreviewAsync([FromBody] GenericPreviewRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetFilePreviewAsync(request, requestorId);
      }

      [HttpPost("message/ciphertext/anon")]
      public async Task<IActionResult> GetAnonymousMessageCiphertextAsync([FromBody] GenericCiphertextRequest request)
      {
         return await DownloadService.GetMessageCiphertextAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("message/ciphertext/auth")]
      public async Task<IActionResult> GetAuthenticatedMessageCiphertextAsync([FromBody] GenericCiphertextRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetMessageCiphertextAsync(request, requestorId);
      }

      [HttpPost("file/ciphertext/anon")]
      public async Task<IActionResult> GetAnonymousFileCiphertext([FromBody] GenericCiphertextRequest request)
      {
         return await DownloadService.GetFileCiphertextAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("file/ciphertext/auth")]
      public async Task<IActionResult> GetAuthenticatedFileCiphertextAsync([FromBody] GenericCiphertextRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetFileCiphertextAsync(request, requestorId);
      }

      [HttpPost("message/signature/anon")]
      public async Task<IActionResult> GetAnonymousMessageSignatureAsync([FromBody] GenericSignatureRequest request)
      {
         return await DownloadService.GetMessageSignatureAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("message/signature/auth")]
      public async Task<IActionResult> GetAuthenticatedMessageSignatureAsync([FromBody] GenericSignatureRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetMessageSignatureAsync(request, requestorId);
      }

      [HttpPost("file/signature/anon")]
      public async Task<IActionResult> GetAnonymousFileSignatureAsync([FromBody] GenericSignatureRequest request)
      {
         return await DownloadService.GetFileSignatureAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("file/signature/auth")]
      public async Task<IActionResult> GetAuthenticatedFileSignatureAsync([FromBody] GenericSignatureRequest request)
      {
         var requestorId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await DownloadService.GetFileSignatureAsync(request, requestorId);
      }
   }
}
