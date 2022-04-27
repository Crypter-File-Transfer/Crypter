/*
 * Copyright (C) 2022 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.API.Attributes;
using Crypter.API.Services;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
using Crypter.Contracts.Features.Transfer.Upload;
using Crypter.Core.Interfaces;
using Crypter.CryptoLib.Services;
using Microsoft.AspNetCore.Http;
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
          IBaseTransferService<IMessageTransferItem> messageService,
          IBaseTransferService<IFileTransferItem> fileService,
          IUserService userService,
          IUserProfileService userProfileService,
          IEmailService emailService,
          IApiValidationService apiValidationService,
          ISimpleEncryptionService simpleEncryptionService,
          ISimpleHashService simpleHashService,
          ITokenService tokenService
         )
      {
         _uploadService = new UploadService(configuration, messageService, fileService, emailService, apiValidationService, simpleEncryptionService, userService, simpleHashService);
         _downloadService = new DownloadService(configuration, messageService, fileService, userService, userProfileService, simpleEncryptionService, simpleHashService);
         _tokenService = tokenService;
      }

      [HttpPost("message")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> MessageTransferAsync([FromBody] UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _uploadService.ReceiveMessageTransferAsync(request, senderId, string.Empty, cancellationToken);
      }

      [HttpPost("message/{recipient}")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UserMessageTransferAsync([FromBody] UploadMessageTransferRequest request, string recipient, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _uploadService.ReceiveMessageTransferAsync(request, senderId, recipient, cancellationToken);
      }

      [HttpPost("file")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> FileTransferAsync([FromBody] UploadFileTransferRequest request, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _uploadService.ReceiveFileTransferAsync(request, senderId, string.Empty, cancellationToken);
      }

      [HttpPost("file/{recipient}")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadTransferResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UserFileTransferAsync([FromBody] UploadFileTransferRequest request, string recipient, CancellationToken cancellationToken)
      {
         var senderId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _uploadService.ReceiveFileTransferAsync(request, senderId, recipient, cancellationToken);
      }

      [HttpPost("message/preview")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetMessagePreviewAsync([FromBody] DownloadTransferPreviewRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetMessagePreviewAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/preview")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferMessagePreviewResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetFilePreviewAsync([FromBody] DownloadTransferPreviewRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetFilePreviewAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("message/ciphertext")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferCiphertextResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetMessageCiphertextAsync([FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetMessageCiphertextAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/ciphertext")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferCiphertextResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetFileCiphertext([FromBody] DownloadTransferCiphertextRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetFileCiphertextAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("message/signature")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferSignatureResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetMessageSignatureAsync([FromBody] DownloadTransferSignatureRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetMessageSignatureAsync(request, requestorId, cancellationToken);
      }

      [HttpPost("file/signature")]
      [MaybeAuthorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadTransferSignatureResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetFileSignatureAsync([FromBody] DownloadTransferSignatureRequest request, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.TryParseUserId(User)
            .IfNone(Guid.Empty);
         return await _downloadService.GetFileSignatureAsync(request, requestorId, cancellationToken);
      }
   }
}