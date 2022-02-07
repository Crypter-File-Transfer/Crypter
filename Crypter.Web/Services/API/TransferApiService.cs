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

using Crypter.Common.FunctionalTypes;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
using Crypter.Contracts.Features.Transfer.Upload;
using Crypter.Web.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Services.API
{
   public interface ITransferApiService
   {
      Task<Either<ErrorResponse, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, Guid recipient, bool withAuthentication);
      Task<Either<ErrorResponse, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, Guid recipient, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferMessagePreviewResponse>> DownloadMessagePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferSignatureResponse>> DownloadMessageSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferCiphertextResponse>> DownloadMessageCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferFilePreviewResponse>> DownloadFilePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferSignatureResponse>> DownloadFileSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication);
      Task<Either<ErrorResponse, DownloadTransferCiphertextResponse>> DownloadFileCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
   }

   public class TransferApiService : ITransferApiService
   {
      private readonly string BaseTransferUrl;
      private readonly IHttpService HttpService;

      public TransferApiService(AppSettings appSettings, IHttpService httpService)
      {
         BaseTransferUrl = $"{appSettings.ApiBaseUrl}/transfer";
         HttpService = httpService;
      }

      public async Task<Either<ErrorResponse, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, Guid recipient, bool withAuthentication)
      {
         var url = recipient == Guid.Empty
            ? $"{BaseTransferUrl}/message"
            : $"{BaseTransferUrl}/message/{recipient}";
         return await HttpService.PostAsync<UploadTransferResponse>(url, uploadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, Guid recipient, bool withAuthentication)
      {
         var url = recipient == Guid.Empty
            ? $"{BaseTransferUrl}/file"
            : $"{BaseTransferUrl}/file/{recipient}";
         return await HttpService.PostAsync<UploadTransferResponse>(url, uploadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferMessagePreviewResponse>> DownloadMessagePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/message/preview";
         return await HttpService.PostAsync<DownloadTransferMessagePreviewResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferSignatureResponse>> DownloadMessageSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/message/signature";
         return await HttpService.PostAsync<DownloadTransferSignatureResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferCiphertextResponse>> DownloadMessageCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/message/ciphertext";
         return await HttpService.PostAsync<DownloadTransferCiphertextResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferFilePreviewResponse>> DownloadFilePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/file/preview";
         return await HttpService.PostAsync<DownloadTransferFilePreviewResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferSignatureResponse>> DownloadFileSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/file/signature";
         return await HttpService.PostAsync<DownloadTransferSignatureResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<Either<ErrorResponse, DownloadTransferCiphertextResponse>> DownloadFileCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         var url = $"{BaseTransferUrl}/file/ciphertext";
         return await HttpService.PostAsync<DownloadTransferCiphertextResponse>(url, downloadRequest, withAuthentication);
      }
   }
}
