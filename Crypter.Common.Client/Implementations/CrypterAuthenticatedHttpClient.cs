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

using Crypter.Common.Client.DeviceStorage.Models;
using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Monads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations
{
   public class CrypterAuthenticatedHttpClient : ICrypterAuthenticatedHttpClient
   {
      private readonly HttpClient _httpClient;
      private readonly ICrypterApiClient _crypterApiClient;
      private readonly ITokenRepository _tokenRepository;
      private readonly JsonSerializerOptions _jsonSerializerOptions;

      private readonly SemaphoreSlim _requestSemaphore = new(1);
      private readonly Dictionary<bool, Func<Task<Maybe<TokenObject>>>> _tokenProviderMap;

      public CrypterAuthenticatedHttpClient(HttpClient httpClient, ITokenRepository tokenRepository, ICrypterApiClient crypterApiClient)
      {
         _httpClient = httpClient;
         _tokenRepository = tokenRepository;
         _crypterApiClient = crypterApiClient;
         _jsonSerializerOptions = new JsonSerializerOptions
         {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
         };

         _tokenProviderMap = new()
         {
            { false, _tokenRepository.GetAuthenticationTokenAsync },
            { true, _tokenRepository.GetRefreshTokenAsync }
         };
      }

      public Task<Either<ErrorResponse, TResponse>> GetAsync<TResponse>(string uri)
         where TResponse : class
      {
         return GetAsync<TResponse>(uri);
      }

      public async Task<Either<ErrorResponse, TResponse>> GetAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Get, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetWithStatusCodeAsync<TResponse>(string uri)
         where TResponse : class
      {
         return GetWithStatusCodeAsync<TResponse>(uri);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetWithStatusCodeAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Get, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PutAsync<TRequest, TResponse>(string uri, TRequest body)
         where TRequest : class
         where TResponse : class
      {
         return PutAsync<TRequest, TResponse>(uri, body, false);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PutAsync<TRequest, TResponse>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Put, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      public async Task<Either<ErrorResponse, TResponse>> PostAsync<TResponse>(string uri, bool useRefreshToken = false) where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<Either<ErrorResponse, TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest body)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, false);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<Maybe<Unit>> PostMaybeUnitResponseAsync(string uri)
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, false);
         return DeserializeMaybeUnitResponseAsync(response);
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri)
      {
         return PostEitherUnitResponseAsync(uri, false);
      }

      public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri, bool useRefreshToken = false)
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body)
         where TRequest : class
      {
         return PostEitherUnitResponseAsync(uri, body, false);
      }

      public async Task<Either<ErrorResponse, Unit>> PostUnitResponseAsync<TRequest>(string uri, bool useRefreshToken = false)
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostWithStatusCodeAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostWithStatusCodeAsync<TRequest, TResponse>(string uri, TRequest body)
         where TRequest : class
         where TResponse : class
      {
         return PostWithStatusCodeAsync<TRequest, TResponse>(uri, body, false);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostWithStatusCodeAsync<TRequest, TResponse>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> response)> PostWithStreamResponseAsync<TRequest>(string uri, TRequest body)
         where TRequest : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         HttpResponseMessage response = await SendWithAuthenticationAsync(request, false);
         return await GetStreamResponseAsync(response);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeleteAsync<TRequest, TResponse>(string uri, TRequest body)
         where TRequest : class
         where TResponse : class
      {
         return DeleteAsync<TRequest, TResponse>(uri, body, false);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeleteAsync<TRequest, TResponse>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Delete, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(request, useRefreshToken);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      public async Task<Either<ErrorResponse, TResponse>> SendAsync<TResponse>(HttpRequestMessage requestMessage)
         where TResponse : class
      {
         using HttpResponseMessage response = await SendWithAuthenticationAsync(() => requestMessage, false);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> SendWithStatusCodeAsync<TResponse>(HttpRequestMessage requestMessage)
         where TResponse : class
      {
         using HttpResponseMessage response = await SendWithAuthenticationAsync(() => requestMessage, false);
         return await DeserializeResponseWithStatusCodeAsync<TResponse>(response);
      }

      private static Func<HttpRequestMessage> MakeRequestMessageFactory(HttpMethod method, string uri)
      {
         return () => new HttpRequestMessage(method, uri);
      }

      private static Func<HttpRequestMessage> MakeRequestMessageFactory<TRequest>(HttpMethod method, string uri, TRequest body)
         where TRequest : class
      {
         return () => new HttpRequestMessage(method, uri)
         {
            Content = JsonContent.Create(body)
         };
      }

      private async Task<HttpResponseMessage> SendWithAuthenticationAsync(Func<HttpRequestMessage> requestFactory, bool useRefreshToken)
      {
         if (!useRefreshToken || _requestSemaphore.CurrentCount != 0)
         {
            await _requestSemaphore.WaitAsync().ConfigureAwait(false);
            _requestSemaphore.Release();
         }

         var initialRequest = requestFactory();
         await AttachTokenAsync(initialRequest, useRefreshToken);
         HttpResponseMessage initialAttempt = await _httpClient.SendAsync(initialRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
         if (initialAttempt.StatusCode != HttpStatusCode.Unauthorized || useRefreshToken)
         {
            return initialAttempt;
         }
         else
         {
            await _requestSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
               var retryRequest = requestFactory();
               var refreshAndRetry = from refreshResponse in _crypterApiClient.UserAuthentication.RefreshSessionAsync()
                                     from unit0 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreAuthenticationTokenAsync(refreshResponse.AuthenticationToken))
                                     from unit1 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreRefreshTokenAsync(refreshResponse.RefreshToken, refreshResponse.RefreshTokenType))
                                     from unit2 in Either<RefreshError, Unit>.FromRightAsync(AttachTokenAsync(retryRequest, false))
                                     from secondAttempt in Either<RefreshError, HttpResponseMessage>.FromRightAsync(_httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead))
                                     select secondAttempt;

               return await refreshAndRetry.MatchAsync(
                  initialAttempt,
                  right => right);
            }
            finally
            {
               _requestSemaphore.Release();
            }
         }
      }

      private Maybe<Unit> DeserializeMaybeUnitResponseAsync(HttpResponseMessage response)
      {
         return response.IsSuccessStatusCode
            ? Unit.Default
            : Maybe<Unit>.None;
      }

      private async Task<Either<ErrorResponse, Unit>> DeserializeEitherUnitResponseAsync(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return Either<ErrorResponse, Unit>.Neither;
         }

         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         if (response.StatusCode != HttpStatusCode.OK)
         {
            return await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
         }

         return Unit.Default;
      }

      private async Task<Either<ErrorResponse, TResponse>> DeserializeResponseAsync<TResponse>(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return Either<ErrorResponse, TResponse>.Neither;
         }

         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         return response.StatusCode == HttpStatusCode.OK
            ? await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false)
            : await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeserializeResponseWithStatusCodeAsync<TResponse>(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return (response.StatusCode, Either<ErrorResponse, TResponse>.Neither);
         }

         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         if (response.StatusCode != HttpStatusCode.OK)
         {
            ErrorResponse error = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            return (response.StatusCode, error);
         }

         TResponse content = await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
         return (response.StatusCode, content);
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> response)> GetStreamResponseAsync(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return (response.StatusCode, Either<ErrorResponse, StreamDownloadResponse>.Neither);
         }

         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         if (response.StatusCode != HttpStatusCode.OK)
         {
            ErrorResponse error = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            return (response.StatusCode, error);
         }

         StreamDownloadResponse responseData = new StreamDownloadResponse(stream, response.Content.Headers.ContentLength!.Value);
         return (response.StatusCode, responseData);
      }

      private async Task<Unit> AttachTokenAsync(HttpRequestMessage request, bool useRefreshToken = false)
      {
         Maybe<TokenObject> tokenData = await _tokenProviderMap[useRefreshToken]();
         request.Headers.Authorization = tokenData.Match(
            () => throw new InvalidOperationException("Token repository does not contain a matching token."),
            x => new AuthenticationHeaderValue("Bearer", x.Token));
         return Unit.Default;
      }
   }
}
