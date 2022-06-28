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

using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Transfer;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Crypter.API.Controllers
{
   public abstract class CrypterController : ControllerBase
   {
      protected IActionResult ServerError(ErrorResponse error) =>
         StatusCode((int)HttpStatusCode.InternalServerError, error);

      protected IActionResult MakeErrorResponse(UploadTransferError error)
      {
         var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
         return error switch
         {
            UploadTransferError.UnknownError => ServerError(errorResponse),
            UploadTransferError.InvalidRequestedLifetimeHours
               or UploadTransferError.OutOfSpace => BadRequest(errorResponse),
            UploadTransferError.RecipientNotFound => NotFound(errorResponse)
         };
#pragma warning restore CS8524
      }

      protected IActionResult MakeErrorResponse(DownloadTransferPreviewError error)
      {
         var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
         return error switch
         {
            DownloadTransferPreviewError.UnknownError => ServerError(errorResponse),
            DownloadTransferPreviewError.NotFound => NotFound(errorResponse)
         };
#pragma warning restore CS8524
      }

      protected IActionResult MakeErrorResponse(DownloadTransferCiphertextError error)
      {
         var errorResponse = new ErrorResponse(error);
#pragma warning disable CS8524
         return error switch
         {
            DownloadTransferCiphertextError.UnknownError => ServerError(errorResponse),
            DownloadTransferCiphertextError.NotFound => NotFound(errorResponse)
         };
#pragma warning restore CS8524
      }
   }
}
