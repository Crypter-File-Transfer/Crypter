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

using Crypter.Web.Models;
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
      Task<(HttpStatusCode HttpStatus, T Payload)> Get<T>(string uri, bool withAuthorization = false);
      Task<(HttpStatusCode HttpStatus, T Payload)> Post<T>(string uri, object value, bool withAuthorization = false);
   }

   public class HttpService : IHttpService
   {
      private readonly HttpClient HttpClient;
      private readonly NavigationManager NavigationManager;
      private readonly ILocalStorageService LocalStorage;

      public HttpService(
          HttpClient httpClient,
          NavigationManager navigationManager,
          ILocalStorageService localStorage
      )
      {
         HttpClient = httpClient;
         NavigationManager = navigationManager;
         LocalStorage = localStorage;
      }

      public async Task<(HttpStatusCode HttpStatus, T Payload)> Get<T>(string uri, bool withAuthorization)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, uri);
         return await SendRequest<T>(request, withAuthorization);
      }

      public async Task<(HttpStatusCode HttpStatus, T Payload)> Post<T>(string uri, object value, bool withAuthorization)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, uri)
         {
            Content = JsonContent.Create(value)
         };
         return await SendRequest<T>(request, withAuthorization);
      }

      private async Task<(HttpStatusCode HttpStatus, T Payload)> SendRequest<T>(HttpRequestMessage request, bool withAuthorization)
      {

         if (withAuthorization)
         {
            var token = (await LocalStorage.GetItem<UserSession>(StoredObjectType.UserSession))?.Token;
            if (string.IsNullOrEmpty(token))
            {
               return await HandleMissingUserAsync<T>();
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
         }

         HttpResponseMessage response;
         try
         {
            response = await HttpClient.SendAsync(request);
         }
         catch (Exception)
         {
            return (HttpStatusCode.ServiceUnavailable, default);
         }

         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return await HandleMissingUserAsync<T>();
         }

         T content = await response.Content.ReadFromJsonAsync<T>();
         response.Dispose();

         return (response.StatusCode, content);
      }

      private async Task<(HttpStatusCode HttpStatus, T Payload)> HandleMissingUserAsync<T>()
      {
         await LocalStorage.Dispose();
         NavigationManager.NavigateTo("/");
         return (HttpStatusCode.Unauthorized, default);
      }
   }
}
