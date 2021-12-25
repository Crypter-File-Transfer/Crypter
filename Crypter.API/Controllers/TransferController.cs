/*
 * Copyright (C) 2021 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.API.Services;
using Crypter.Contracts.Requests;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.CryptoLib.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [Route("api/transfer")]
   public class TransferController : ControllerBase
   {
      private readonly UploadService _uploadService;
      private readonly DownloadService _downloadService;
      private readonly ITokenService _tokenService;

      public TransferController(IConfiguration configuration,
          IBaseTransferService<MessageTransfer> messageService,
          IBaseTransferService<FileTransfer> fileService,
          IUserService userService,
          IUserProfileService userProfileService,
          IEmailService emailService,
          IApiValidationService apiValidationService,
          ISimpleEncryptionService simpleEncryptionService,
          ISimpleHashService simpleHashService,
          ITokenService tokenService
         )
      {
         _uploadService = new UploadService(configuration, messageService, fileService, emailService, apiValidationService, simpleEncryptionService, simpleHashService);
         _downloadService = new DownloadService(configuration, messageService, fileService, userService, userProfileService, simpleEncryptionService, simpleHashService);
         _tokenService = tokenService;
      }

      [HttpPost("message")]
      public async Task<IActionResult> MessageTransferAsync([FromBody] MessageTransferRequest request, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.ParseUserId(User);
         return await _uploadService.ReceiveMessageTransferAsync(request, senderId, Guid.Empty, cancellationToken);
      }

      [HttpPost("message/{recipientId}")]
      public async Task<IActionResult> UserMessageTransferAsync([FromBody] MessageTransferRequest request, Guid recipientId, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.ParseUserId(User);
         return await _uploadService.ReceiveMessageTransferAsync(request, senderId, recipientId, cancellationToken);
      }

      [HttpPost("file")]
      public async Task<IActionResult> FileTransferAsync([FromBody] FileTransferRequest request, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.ParseUserId(User);
         return await _uploadService.ReceiveFileTransferAsync(request, senderId, Guid.Empty, cancellationToken);
      }

      [HttpPost("file/{recipientId}")]
      public async Task<IActionResult> UserFileTransferAsync([FromBody] FileTransferRequest request, Guid recipientId, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.ParseUserId(User);
         return await _uploadService.ReceiveFileTransferAsync(request, senderId, recipientId, cancellationToken);
      }

      [HttpPost("message/preview")]
      public async Task<IActionResult> GetMessagePreviewAsync([FromBody] GetTransferPreviewRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetMessagePreviewAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/preview")]
      public async Task<IActionResult> GetFilePreviewAsync([FromBody] GetTransferPreviewRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetFilePreviewAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("message/ciphertext")]
      public async Task<IActionResult> GetMessageCiphertextAsync([FromBody] GetTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetMessageCiphertextAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/ciphertext")]
      public async Task<IActionResult> GetFileCiphertext([FromBody] GetTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetFileCiphertextAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("message/signature")]
      public async Task<IActionResult> GetMessageSignatureAsync([FromBody] GetTransferSignatureRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetMessageSignatureAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/signature")]
      public async Task<IActionResult> GetFileSignatureAsync([FromBody] GetTransferSignatureRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         return await _downloadService.GetFileSignatureAsync(request, requestorId, cancellationToken);
      }
   }
}