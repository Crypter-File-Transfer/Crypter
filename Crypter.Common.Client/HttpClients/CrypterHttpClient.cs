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

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts;
using EasyMonads;

namespace Crypter.Common.Client.HttpClients;

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

   public async Task<Maybe<TResponse>> GetMaybeAsync<TResponse>(string uri)
      where TResponse : class
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
      return await SendRequestMaybeResponseAsync<TResponse>(request);
   }

   public async Task<Either<ErrorResponse, TResponse>> GetEitherAsync<TResponse>(string uri)
      where TResponse : class
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
      return await SendRequestEitherResponseAsync<TResponse>(request);
   }

   public async Task<Either<ErrorResponse, Unit>> GetEitherUnitResponseAsync(string uri)
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
      return await SendRequestEitherUnitResponseAsync(request);
   }

   public async Task<Either<ErrorResponse, StreamDownloadResponse>> GetStreamResponseAsync(string uri)
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
      return await GetStreamAsync(request);
   }

   public async Task<Either<ErrorResponse, TResponse>> PostEitherAsync<TRequest, TResponse>(string uri, TRequest body)
      where TResponse : class
      where TRequest : class
   {
      using HttpRequestMessage request = MakeRequestMessage(HttpMethod.Post, uri, body);
      return await SendRequestEitherResponseAsync<TResponse>(request);
   }

   public async Task<Maybe<Unit>> PostMaybeUnitResponseAsync(string uri)
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
      using HttpResponseMessage response = await _httpClient.SendAsync(request);
      return response.IsSuccessStatusCode
         ? Unit.Default
         : Maybe<Unit>.None;
   }

   public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri)
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
      return await SendRequestEitherUnitResponseAsync(request);
   }

   public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body)
      where TRequest : class
   {
      using HttpRequestMessage request = MakeRequestMessage(HttpMethod.Post, uri, body);
      return await SendRequestEitherUnitResponseAsync(request);
   }

   public async Task<Maybe<Unit>> DeleteUnitResponseAsync(string uri)
   {
      using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
      using HttpResponseMessage response = await _httpClient.SendAsync(request);
      return response.IsSuccessStatusCode
         ? Unit.Default
         : Maybe<Unit>.None;
   }

   public async Task<Either<ErrorResponse, TResponse>> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory)
      where TResponse : class
   {
      using HttpRequestMessage request = requestFactory();
      return await SendRequestEitherResponseAsync<TResponse>(request);
   }

   private static HttpRequestMessage MakeRequestMessage<TRequest>(HttpMethod method, string uri, TRequest body)
      where TRequest : class
   {
      return new HttpRequestMessage(method, uri)
      {
         Content = JsonContent.Create(body)
      };
   }

   private async Task<Maybe<TResponse>> SendRequestMaybeResponseAsync<TResponse>(HttpRequestMessage request)
   {
      using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
      using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
      return response.IsSuccessStatusCode
         ? await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false)
         : Maybe<TResponse>.None;
   }

   private async Task<Either<ErrorResponse, TResponse>> SendRequestEitherResponseAsync<TResponse>(HttpRequestMessage request)
   {
      using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
      using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
      return response.IsSuccessStatusCode
         ? await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false)
         : await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
   }

   private async Task<Either<ErrorResponse, Unit>> SendRequestEitherUnitResponseAsync(HttpRequestMessage request)
   {
      using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
      using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
      return response.IsSuccessStatusCode
         ? Unit.Default
         : await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
   }

   private async Task<Either<ErrorResponse, StreamDownloadResponse>> GetStreamAsync(HttpRequestMessage request)
   {
      HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
      Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

      if (response.IsSuccessStatusCode)
      {
         // Do not dispose of the Stream here. The caller needs to read it.
         return new StreamDownloadResponse(stream, response.Content.Headers.ContentLength!.Value);
      }
      else
      {
         ErrorResponse errorResponse = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
         response.Dispose();
         return errorResponse;
      }
   }
}