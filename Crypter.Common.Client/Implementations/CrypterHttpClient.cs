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
using Crypter.Common.Contracts;
using Crypter.Common.Monads;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations
{
   public class CrypterHttpClient : ICrypterHttpClient
   {
      private readonly HttpClient _httpClient;
      private readonly JsonSerializerOptions _jsonSerializerOptions;

      public CrypterHttpClient(HttpClient httpClient)
      {
         _httpClient = httpClient;
         _jsonSerializerOptions = new JsonSerializerOptions
         {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
         };
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetWithStatusCodeAsync<TResponse>(string uri)
         where TResponse : class
      {
         var request = new HttpRequestMessage(HttpMethod.Get, uri);
         return SendRequestWithStatusCodeAsync<TResponse>(request);
      }

      public Task<Either<ErrorResponse, TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest body)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Post, uri, body);
         return SendRequestAsync<TResponse>(request);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostWithStatusCodeAsync<TRequest, TResponse>(string uri, TRequest body)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Post, uri, body);
         return SendRequestWithStatusCodeAsync<TResponse>(request);
      }

      public async Task<Maybe<Unit>> PostMaybeUnitResponseAsync(string uri)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, uri);
         using HttpResponseMessage response = await _httpClient.SendAsync(request);
         return response.IsSuccessStatusCode
            ? Unit.Default
            : Maybe<Unit>.None;
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, uri);
         return SendRequestUnitResponseAsync(request);
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body)
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Post, uri, body);
         return SendRequestUnitResponseAsync(request);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> response)> PostWithStreamResponseAsync<TRequest>(string uri, TRequest body)
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Post, uri, body);
         return GetStreamAsync(request);
      }

      public Task<Either<ErrorResponse, TResponse>> SendAsync<TResponse>(HttpRequestMessage requestMessage)
         where TResponse : class
      {
         return SendRequestAsync<TResponse>(requestMessage);
      }

      private static HttpRequestMessage MakeRequestMessage<TRequest>(HttpMethod method, string uri, TRequest body)
         where TRequest : class
      {
         return new HttpRequestMessage(method, uri)
         {
            Content = JsonContent.Create(body)
         };
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> SendRequestWithStatusCodeAsync<TResponse>(HttpRequestMessage request)
         where TResponse : class
      {
         using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
         {
            Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
               ErrorResponse error = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
               return (response.StatusCode, error);
            }

            TResponse content = await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            return (response.StatusCode, content);
         }
      }

      private async Task<Either<ErrorResponse, TResponse>> SendRequestAsync<TResponse>(HttpRequestMessage request)
      {
         using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
         {
            Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
               return await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            }

            return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
         }
      }

      private async Task<Either<ErrorResponse, Unit>> SendRequestUnitResponseAsync(HttpRequestMessage request)
      {
         using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
         {
            Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.OK
               ? Unit.Default
               : await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
         }
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> response)> GetStreamAsync(HttpRequestMessage request)
      {
         HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

         if (response.StatusCode != HttpStatusCode.OK)
         {
            ErrorResponse error = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            return (response.StatusCode, error);
         }

         StreamDownloadResponse responseData = new StreamDownloadResponse(stream, response.Content.Headers.ContentLength!.Value);
         return (response.StatusCode, responseData);
      }
   }
}
