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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserAuthentication;
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
using EasyMonads;

namespace Crypter.Common.Client.HttpClients
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

      public async Task<Maybe<TResponse>> GetMaybeAsync<TResponse>(string uri)
         where TResponse : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Get, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return await DeserializeMaybeResponseAsync<TResponse>(response);
      }

      public Task<Either<ErrorResponse, TResponse>> GetEitherAsync<TResponse>(string uri)
         where TResponse : class
      {
         return GetEitherAsync<TResponse>(uri, false);
      }

      public async Task<Either<ErrorResponse, TResponse>> GetEitherAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Get, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<Either<ErrorResponse, Unit>> GetEitherUnitResponseAsync(string uri)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Get, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<Either<ErrorResponse, StreamDownloadResponse>> GetStreamResponseAsync(string uri)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Get, uri);

         // Do not dispose of the HttpResponseMessage here.
         // Callers need to read the contained Stream.
         HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return await GetStreamResponseAsync(response);
      }

      public async Task<Either<ErrorResponse, TResponse>> PutEitherAsync<TRequest, TResponse>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Put, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<Either<ErrorResponse, Unit>> PutEitherUnitResponseAsync<TRequest>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Put, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<Either<ErrorResponse, TResponse>> PostEitherAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public Task<Either<ErrorResponse, TResponse>> PostEitherAsync<TRequest, TResponse>(string uri, TRequest body)
         where TResponse : class
         where TRequest : class
      {
         return PostEitherAsync<TRequest, TResponse>(uri, body, false);
      }

      public async Task<Either<ErrorResponse, TResponse>> PostEitherAsync<TRequest, TResponse>(string uri, TRequest body, bool useRefreshToken = false)
         where TResponse : class
         where TRequest : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return await DeserializeResponseAsync<TResponse>(response);
      }

      public async Task<Maybe<Unit>> PostMaybeUnitResponseAsync(string uri)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return DeserializeMaybeUnitResponse(response);
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri)
      {
         return PostEitherUnitResponseAsync(uri, false);
      }

      public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync(string uri, bool useRefreshToken = false)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body)
         where TRequest : class
      {
         return PostEitherUnitResponseAsync(uri, body, false);
      }

      public async Task<Either<ErrorResponse, Unit>> PostUnitResponseAsync<TRequest>(string uri, bool useRefreshToken = false)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<Either<ErrorResponse, Unit>> PostEitherUnitResponseAsync<TRequest>(string uri, TRequest body, bool useRefreshToken = false)
         where TRequest : class
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, useRefreshToken);
         return await DeserializeEitherUnitResponseAsync(response);
      }

      public async Task<Maybe<Unit>> DeleteUnitResponseAsync(string uri)
      {
         var requestFactory = MakeRequestMessageFactory(HttpMethod.Delete, uri);
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return response.IsSuccessStatusCode
            ? Unit.Default
            : Maybe<Unit>.None;
      }

      public async Task<Either<ErrorResponse, TResponse>> SendAsync<TResponse>(Func<HttpRequestMessage> requestFactory)
         where TResponse : class
      {
         using HttpResponseMessage response = await SendWithAuthenticationAsync(requestFactory, false);
         return await DeserializeResponseAsync<TResponse>(response);
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

         using HttpRequestMessage initialRequest = requestFactory();
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
               using HttpRequestMessage retryRequest = requestFactory();
               var refreshAndRetry = await (from refreshResponse in _crypterApiClient.UserAuthentication.RefreshSessionAsync()
                                            from unit0 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreAuthenticationTokenAsync(refreshResponse.AuthenticationToken))
                                            from unit1 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreRefreshTokenAsync(refreshResponse.RefreshToken, refreshResponse.RefreshTokenType))
                                            from unit2 in Either<RefreshError, Unit>.FromRightAsync(AttachTokenAsync(retryRequest, false))
                                            from secondAttempt in Either<RefreshError, HttpResponseMessage>.FromRightAsync(_httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead))
                                            select secondAttempt);

               return refreshAndRetry.Match(
                  initialAttempt,
                  right => right);
            }
            finally
            {
               _requestSemaphore.Release();
            }
         }
      }

      private static Maybe<Unit> DeserializeMaybeUnitResponse(HttpResponseMessage response)
      {
         return response.IsSuccessStatusCode
            ? Unit.Default
            : Maybe<Unit>.None;
      }

      private async Task<Maybe<TResponse>> DeserializeMaybeResponseAsync<TResponse>(HttpResponseMessage response)
      {
         if (!response.IsSuccessStatusCode)
         {
            return Maybe<TResponse>.None;
         }

         using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
      }

      private async Task<Either<ErrorResponse, Unit>> DeserializeEitherUnitResponseAsync(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return Either<ErrorResponse, Unit>.Neither;
         }

         using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         if (!response.IsSuccessStatusCode)
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

         using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
         return response.IsSuccessStatusCode
            ? await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false)
            : await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
      }

      private async Task<Either<ErrorResponse, StreamDownloadResponse>> GetStreamResponseAsync(HttpResponseMessage response)
      {
         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return Either<ErrorResponse, StreamDownloadResponse>.Neither;
         }

         Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

         if (response.IsSuccessStatusCode)
         {
            // Do not dispose of the Stream here. The caller needs to read it.
            return new StreamDownloadResponse(stream, response.Content.Headers.ContentLength!.Value);
         }
         else
         {
            ErrorResponse errorResponse = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, _jsonSerializerOptions).ConfigureAwait(false);
            stream.Dispose();
            return errorResponse;
         }
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
