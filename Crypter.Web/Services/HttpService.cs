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

using Crypter.Web.Models.LocalStorage;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IHttpService
   {
      /// <summary>
      /// Send a GET request to the provided URL.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="uri"></param>
      /// <param name="withAuthorization">If true, will send the request with the Authorization header.</param>
      /// <param name="useRefreshToken">
      /// False by default, will apply the standard authentication token to the Authorization header.
      /// If true, will apply a refresh token to the Authorization header instead.
      /// </param>
      /// <returns></returns>
      Task<(HttpStatusCode HttpStatus, T Response)> GetAsync<T>(string uri, bool withAuthorization = false, bool useRefreshToken = false);

      /// <summary>
      /// Send a POST request to the provided URL with the provided data.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="uri"></param>
      /// <param name="postData"></param>
      /// <param name="withAuthorization">If true, will send the request with the Authorization header.</param>
      /// <param name="useRefreshToken">
      /// False by default, will apply the standard authentication token to the Authorization header.
      /// If true, will apply a refresh token to the Authorization header instead.
      /// </param>
      /// <returns></returns>
      Task<(HttpStatusCode HttpStatus, T Response)> PostAsync<T>(string uri, object postData, bool withAuthorization = false, bool useRefreshToken = false);
   }

   public class HttpService : IHttpService
   {
      private readonly HttpClient _httpClient;
      private readonly NavigationManager _navigationManager;
      private readonly ILocalStorageService _localStorageService;
      private readonly Func<IAuthenticationService> _authenticationServiceFactory;

      public HttpService(
          HttpClient httpClient,
          NavigationManager navigationManager,
          ILocalStorageService localStorage,
          Func<IAuthenticationService> authenticationServiceFactory
      )
      {
         _httpClient = httpClient;
         _navigationManager = navigationManager;
         _localStorageService = localStorage;
         _authenticationServiceFactory = authenticationServiceFactory;
      }

      public async Task<(HttpStatusCode HttpStatus, T Response)> GetAsync<T>(string uri, bool withAuthorization = false, bool useRefreshToken = false)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, uri);

         if (withAuthorization)
         {
            if (!await AttachTokenAsync(request, useRefreshToken))
            {
               return await HandleMissingTokenAsync<T>();
            }
         }

         return await SendRequestAsync<T>(request, useRefreshToken);
      }

      public async Task<(HttpStatusCode HttpStatus, T Response)> PostAsync<T>(string uri, object postData, bool withAuthorization = false, bool useRefreshToken = false)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, uri)
         {
            Content = JsonContent.Create(postData)
         };

         if (withAuthorization)
         {
            if (!await AttachTokenAsync(request, useRefreshToken))
            {
               return await HandleMissingTokenAsync<T>();
            }
         }

         return await SendRequestAsync<T>(request, useRefreshToken);
      }

      private async Task<bool> AttachTokenAsync(HttpRequestMessage request, bool attachRefreshToken = false)
      {
         var token = attachRefreshToken
               ? (await _localStorageService.GetItemAsync<UserSession>(StoredObjectType.UserSession)).RefreshToken
               : await _localStorageService.GetItemAsync<string>(StoredObjectType.AuthenticationToken);

         if (string.IsNullOrEmpty(token))
         {
            return false;
         }

         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
         return true;
      }

      private async Task<(HttpStatusCode HttpStatus, T Response)> SendRequestAsync<T>(HttpRequestMessage request, bool isUsingRefreshToken)
      {
         HttpResponseMessage response;
         try
         {
            response = await _httpClient.SendAsync(request);
         }
         catch (Exception)
         {
            return (HttpStatusCode.ServiceUnavailable, default);
         }

         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            if (!isUsingRefreshToken && await TryRefreshingTokenAsync())
            {
               await AttachTokenAsync(request);
               return await SendRequestAsync<T>(request, false);
            }

            return await HandleMissingTokenAsync<T>();
         }

         T content = await response.Content.ReadFromJsonAsync<T>();
         response.Dispose();

         return (response.StatusCode, content);
      }

      private async Task<(HttpStatusCode HttpStatus, T Response)> HandleMissingTokenAsync<T>()
      {
         await _localStorageService.DisposeAsync();
         _navigationManager.NavigateTo("/");
         return (HttpStatusCode.Unauthorized, default);
      }

      private async Task<bool> TryRefreshingTokenAsync()
      {
         return await _authenticationServiceFactory().TryRefreshingTokenAsync();
      }
   }
}
