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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Implementations
{
   public partial class CrypterApiHttpService : ICrypterApiHttpService
   {
      private readonly HttpClient _httpClient;

      public CrypterApiHttpService(HttpClient httpClient)
      {
         _httpClient = httpClient;
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> GetAsync<TResponse>(string uri, Maybe<string> bearerToken)
         where TResponse : class
      {
         var request = MakeRequestMessage(HttpMethod.Get, uri, Maybe<object>.None, bearerToken);
         return await SendRequestAsync<TResponse>(request);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PutAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, Maybe<string> bearerToken)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Put, uri, body, bearerToken);
         return await SendRequestAsync<TResponse>(request);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> PostAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, Maybe<string> bearerToken)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Post, uri, body, bearerToken);
         return await SendRequestAsync<TResponse>(request);
      }

      public async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> DeleteAsync<TRequest, TResponse>(string uri, Maybe<TRequest> body, Maybe<string> bearerToken)
         where TResponse : class
         where TRequest : class
      {
         var request = MakeRequestMessage(HttpMethod.Delete, uri, body, bearerToken);
         return await SendRequestAsync<TResponse>(request);
      }

      private static HttpRequestMessage MakeRequestMessage<TRequest>(HttpMethod method, string uri, Maybe<TRequest> body, Maybe<string> bearerToken)
         where TRequest : class
      {
         var message = new HttpRequestMessage(method, uri)
         {
            Content = body.Match(
               () => null,
               x => JsonContent.Create(x)),
         };

         message.Headers.Authorization = bearerToken.Match(
            () => null,
            x => new AuthenticationHeaderValue("Bearer", x));

         return message;
      }

      private async Task<(HttpStatusCode httpStatus, Either<ErrorResponse, TResponse> response)> SendRequestAsync<TResponse>(HttpRequestMessage request)
         where TResponse : class
      {
         using HttpResponseMessage response = await _httpClient.SendAsync(request);

         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return (response.StatusCode, new());
         }

         if (response.StatusCode != HttpStatusCode.OK)
         {
            ErrorResponse error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (response.StatusCode, new(error));
         }

         TResponse content = await response.Content.ReadFromJsonAsync<TResponse>();
         return (response.StatusCode, new(content));
      }
   }
}
