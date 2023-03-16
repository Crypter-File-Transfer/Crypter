/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Monads;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations.Requests
{
   public class MessageTransferRequests : IMessageTransferRequests
   {
      private readonly ICrypterHttpClient _crypterHttpClient;
      private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

      public MessageTransferRequests(ICrypterHttpClient crypterHttpClient, ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient)
      {
         _crypterHttpClient = crypterHttpClient;
         _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(Maybe<string> recipientUsername, UploadMessageTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication)
      {
         string url = recipientUsername.Match(
            () => "api/message/transfer",
            x => $"api/message/transfer?username={x}");

         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
         {
            Content = new MultipartFormDataContent
            {
               { new StringContent(JsonSerializer.Serialize(uploadRequest), Encoding.UTF8, "application/json"), "Data" },
               { new StreamContent(encryptionStream), "Ciphertext", "Ciphertext" }
            }
         };

         return await service.SendAsync<UploadTransferResponse>(request)
            .ExtractErrorCode<UploadTransferError, UploadTransferResponse>();
      }

      public Task<Maybe<List<UserReceivedMessageDTO>>> GetReceivedMessagesAsync()
      {
         string url = "/api/message/transfer/received";
         return _crypterAuthenticatedHttpClient.GetMaybeAsync<List<UserReceivedMessageDTO>>(url);
      }

      public Task<Maybe<List<UserSentMessageDTO>>> GetSentMessagesAsync()
      {
         string url = "/api/message/transfer/sent";
         return _crypterAuthenticatedHttpClient.GetMaybeAsync<List<UserSentMessageDTO>>(url);
      }

      public Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetAnonymousMessagePreviewAsync(string hashId)
      {
         string url = $"api/message/transfer/preview/anonymous?id={hashId}";
         return _crypterHttpClient.GetEitherAsync<MessageTransferPreviewResponse>(url)
            .ExtractErrorCode<TransferPreviewError, MessageTransferPreviewResponse>();
      }

      public Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetUserMessagePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = $"api/message/transfer/preview/user?id={hashId}";

         ICrypterHttpClient client = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         return client.GetEitherAsync<MessageTransferPreviewResponse>(url)
            .ExtractErrorCode<TransferPreviewError, MessageTransferPreviewResponse>();
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> GetAnonymousMessageCiphertextAsync(string hashId, byte[] proof)
      {
         string url = $"api/message/transfer/ciphertext/anonymous?id={hashId}&proof={Convert.ToBase64String(proof)}";
         return _crypterHttpClient.GetStreamResponseAsync(url)
            .ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>();
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> GetUserMessageCiphertextAsync(string hashId, byte[] proof, bool withAuthentication)
      {
         string url = $"api/message/transfer/ciphertext/user?id={hashId}&proof={Convert.ToBase64String(proof)}";

         ICrypterHttpClient client = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         return client.GetStreamResponseAsync(url)
            .ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>();
      }
   }
}
