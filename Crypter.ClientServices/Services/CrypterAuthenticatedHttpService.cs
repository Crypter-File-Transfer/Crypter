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

using Crypter.ClientServices.DeviceStorage.Models;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class CrypterAuthenticatedHttpService : ICrypterAuthenticatedHttpService
   {
      private readonly HttpClient _httpClient;
      private readonly Func<ICrypterApiService> _crypterApiFactory;
      private readonly ITokenRepository _tokenRepository;

      private readonly SemaphoreSlim _requestSemaphore = new(1);
      private readonly Dictionary<bool, Func<Task<Maybe<TokenObject>>>> _tokenProviderMap;

      public CrypterAuthenticatedHttpService(HttpClient httpClient, ITokenRepository tokenRepository, Func<ICrypterApiService> crypterApiFactory)
      {
         _httpClient = httpClient;
         _tokenRepository = tokenRepository;
         _crypterApiFactory = crypterApiFactory;

         _tokenProviderMap = new()
         {
            { false, _tokenRepository.GetAuthenticationTokenAsync },
            { true, _tokenRepository.GetRefreshTokenAsync }
         };
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetAsync<TResponse>(string uri)
         where TResponse : class
      {
         return GetAsync<TResponse>(uri, false);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Get, uri, Maybe<object>.None);
         return SendWithAuthenticationAsync<TResponse>(request, useRefreshToken);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PutAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body)
         where TRequest : class
         where TResponse : class
      {
         return PutAsync<TRequest, TResponse>(uri, body, false);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PutAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Put, uri, body);
         return SendWithAuthenticationAsync<TResponse>(request, useRefreshToken);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostAsync<TResponse>(string uri, bool useRefreshToken = false)
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, Maybe<object>.None);
         return SendWithAuthenticationAsync<TResponse>(request, useRefreshToken);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body)
         where TRequest : class
         where TResponse : class
      {
         return PostAsync<TRequest, TResponse>(uri, body, false);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Post, uri, body);
         return SendWithAuthenticationAsync<TResponse>(request, useRefreshToken);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeleteAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body)
         where TRequest : class
         where TResponse : class
      {
         return DeleteAsync<TRequest, TResponse>(uri, body, false);
      }

      public Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeleteAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, bool useRefreshToken = false)
         where TRequest : class
         where TResponse : class
      {
         var request = MakeRequestMessageFactory(HttpMethod.Delete, uri, body);
         return SendWithAuthenticationAsync<TResponse>(request, useRefreshToken);
      }

      private static Func<HttpRequestMessage> MakeRequestMessageFactory<TRequest>(HttpMethod method, string uri, Maybe<TRequest> body)
         where TRequest : class
      {
         return () => new HttpRequestMessage(method, uri)
         {
            Content = body.Match(
               () => null,
               x => JsonContent.Create(x))
         };
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> SendWithAuthenticationAsync<TResponse>(Func<HttpRequestMessage> requestFactory, bool useRefreshToken)
      {
         if (!useRefreshToken || _requestSemaphore.CurrentCount != 0)
         {
            await _requestSemaphore.WaitAsync().ConfigureAwait(false);
            _requestSemaphore.Release();
         }

         var initialRequest = requestFactory();
         await AttachTokenAsync(initialRequest, useRefreshToken);
         var initialAttempt = await SendRequestAsync<TResponse>(initialRequest);
         if (initialAttempt.httpStatus != HttpStatusCode.Unauthorized || useRefreshToken)
         {
            return initialAttempt;
         }
         else
         {
            await _requestSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
               var retryRequest = requestFactory();
               var refreshAndRetry = from refreshResponse in _crypterApiFactory().RefreshAsync()
                                     from unit0 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreAuthenticationTokenAsync(refreshResponse.AuthenticationToken))
                                     from unit1 in Either<RefreshError, Unit>.FromRightAsync(_tokenRepository.StoreRefreshTokenAsync(refreshResponse.RefreshToken, refreshResponse.RefreshTokenType))
                                     from unit2 in Either<RefreshError, Unit>.FromRightAsync(AttachTokenAsync(retryRequest, false))
                                     from secondAttempt in Either<RefreshError, (HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)>.FromRightAsync(SendRequestAsync<TResponse>(retryRequest))
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

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> SendRequestAsync<TResponse>(HttpRequestMessage request)
      {
         using HttpResponseMessage response = await _httpClient.SendAsync(request);

         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return (response.StatusCode, Either<ErrorResponse, TResponse>.Neither);
         }

         if (response.StatusCode != HttpStatusCode.OK)
         {
            ErrorResponse error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (response.StatusCode, error);
         }

         TResponse content = await response.Content.ReadFromJsonAsync<TResponse>();
         return (response.StatusCode, content);
      }

      private async Task<Unit> AttachTokenAsync(HttpRequestMessage request, bool useRefreshToken = false)
      {
         Maybe<TokenObject> tokenData = await _tokenProviderMap[useRefreshToken]();
         request.Headers.Authorization = tokenData.Match(
            () => null,
            x => new AuthenticationHeaderValue("Bearer", x.Token));
         return Unit.Default;
      }
   }
}
