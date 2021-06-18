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
   [Route("api/upload")]
   public class UploadController : ControllerBase
   {
      private readonly UploadService UploadService;

      public UploadController(IConfiguration configuration,
          IBaseItemService<MessageItem> messageService,
          IBaseItemService<FileItem> fileService,
          IUserService userService
         )
      {
         UploadService = new UploadService(configuration, messageService, fileService, userService);
      }

      [HttpPost("message/anon")]
      public async Task<IActionResult> AnonymousMessageUploadAsync([FromBody] MessageUploadRequest request)
      {
         return await UploadService.UploadMessageAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("message/auth")]
      public async Task<IActionResult> AuthenticatedMessageUploadAsync([FromBody] MessageUploadRequest request)
      {
         var senderId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await UploadService.UploadMessageAsync(request, senderId);
      }

      [HttpPost("file/anon")]
      public async Task<IActionResult> AnonymousFileUploadAsync([FromBody] FileUploadRequest request)
      {
         return await UploadService.UploadFileAsync(request, Guid.Empty);
      }

      [Authorize]
      [HttpPost("file/auth")]
      public async Task<IActionResult> AuthenticatedFileUploadAsync([FromBody] FileUploadRequest request)
      {
         var senderId = Guid.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
         return await UploadService.UploadFileAsync(request, senderId);
      }
   }
}