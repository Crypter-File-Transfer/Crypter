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

using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Services.API
{
   public interface IAuthenticationApiService
   {
      Task<(HttpStatusCode HttpStatus, LoginResponse Response)> LoginAsync(LoginRequest loginRequest);
      Task<(HttpStatusCode HttpStatus, RefreshResponse Response)> RefreshAsync();
      Task<(HttpStatusCode HttpStatus, object _)> LogoutAsync(LogoutRequest logoutRequest);
   }

   public class AuthenticationApiService : IAuthenticationApiService
   {
      private readonly string BaseAuthenticationUrl;
      private readonly IHttpService HttpService;

      public AuthenticationApiService(AppSettings appSettings, IHttpService httpService)
      {
         BaseAuthenticationUrl = $"{appSettings.ApiBaseUrl}/authentication";
         HttpService = httpService;
      }

      public async Task<(HttpStatusCode, LoginResponse)> LoginAsync(LoginRequest loginRequest)
      {
         var url = $"{BaseAuthenticationUrl}/login";
         return await HttpService.PostAsync<LoginResponse>(url, loginRequest);
      }

      public async Task<(HttpStatusCode, RefreshResponse)> RefreshAsync()
      {
         var url = $"{BaseAuthenticationUrl}/refresh";
         return await HttpService.GetAsync<RefreshResponse>(url, true, true);
      }

      public async Task<(HttpStatusCode, object)> LogoutAsync(LogoutRequest logoutRequest)
      {
         var url = $"{BaseAuthenticationUrl}/logout";
         return await HttpService.PostAsync<object>(url, logoutRequest, true);
      }
   }
}
