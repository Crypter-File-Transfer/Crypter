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
   public interface IUserApiService
   {
      Task<(HttpStatusCode HttpStatus, UserAuthenticateResponse Response)> AuthenticateUserAsync(AuthenticateUserRequest loginRequest);
      Task<(HttpStatusCode HttpStatus, UserAuthenticationRefreshResponse Response)> RefreshAuthenticationAsync();
      Task<(HttpStatusCode HttpStatus, UserRegisterResponse Response)> RegisterUserAsync(RegisterUserRequest registerRequest);
      Task<(HttpStatusCode HttpStatus, UserPublicProfileResponse Response)> GetUserPublicProfileAsync(string username, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, UserSettingsResponse Response)> GetUserSettingsAsync();
      Task<(HttpStatusCode HttpStatus, UpdateProfileResponse Response)> UpdateUserProfileInfoAsync(UpdateProfileRequest request);
      Task<(HttpStatusCode HttpStatus, UpdateContactInfoResponse Response)> UpdateUserContactInfoAsync(UpdateContactInfoRequest request);
      Task<(HttpStatusCode HttpStatus, UpdatePrivacySettingResponse Response)> UpdateUserPrivacyAsync(UpdatePrivacySettingRequest request);
      Task<(HttpStatusCode HttpStatus, UpdateNotificationSettingResponse Response)> UpdateUserNotificationAsync(UpdateNotificationSettingRequest request);
      Task<(HttpStatusCode HttpStatus, UpdateKeysResponse Response)> InsertUserX25519KeysAsync(UpdateKeysRequest request);
      Task<(HttpStatusCode HttpStatus, UpdateKeysResponse Response)> InsertUserEd25519KeysAsync(UpdateKeysRequest request);
      Task<(HttpStatusCode HttpStatus, UserSentMessagesResponse Response)> GetUserSentMessagesAsync();
      Task<(HttpStatusCode HttpStatus, UserSentFilesResponse Response)> GetUserSentFilesAsync();
      Task<(HttpStatusCode HttpStatus, UserReceivedMessagesResponse Response)> GetUserReceivedMessagesAsync();
      Task<(HttpStatusCode HttpStatus, UserReceivedFilesResponse Response)> GetUserReceivedFilesAsync();
      Task<(HttpStatusCode HttpStatus, UserSearchResponse Response)> GetUserSearchResultsAsync(UserSearchParams searchInfo);
      Task<(HttpStatusCode HttpStatus, UserEmailVerificationResponse Response)> VerifyUserEmailAddressAsync(VerifyUserEmailAddressRequest verificationInfo);
   }

   public class UserApiService : IUserApiService
   {
      private readonly string BaseUserUrl;
      private readonly IHttpService HttpService;

      public UserApiService(AppSettings appSettings, IHttpService httpService)
      {
         BaseUserUrl = $"{appSettings.ApiBaseUrl}/user";
         HttpService = httpService;
      }

      public async Task<(HttpStatusCode, UserAuthenticateResponse)> AuthenticateUserAsync(AuthenticateUserRequest loginRequest)
      {
         var url = $"{BaseUserUrl}/authenticate";
         return await HttpService.Post<UserAuthenticateResponse>(url, loginRequest);
      }

      public async Task<(HttpStatusCode, UserAuthenticationRefreshResponse)> RefreshAuthenticationAsync()
      {
         var url = $"{BaseUserUrl}/authenticate/refresh";
         return await HttpService.Get<UserAuthenticationRefreshResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserRegisterResponse)> RegisterUserAsync(RegisterUserRequest registerRequest)
      {
         var url = $"{BaseUserUrl}/register";
         return await HttpService.Post<UserRegisterResponse>(url, registerRequest);
      }

      public async Task<(HttpStatusCode, UserPublicProfileResponse)> GetUserPublicProfileAsync(string username, bool withAuthentication)
      {
         var url = $"{BaseUserUrl}/{username}";
         return await HttpService.Get<UserPublicProfileResponse>(url, withAuthentication);
      }

      public async Task<(HttpStatusCode, UserSettingsResponse)> GetUserSettingsAsync()
      {
         var url = $"{BaseUserUrl}/settings";
         return await HttpService.Get<UserSettingsResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UpdateProfileResponse)> UpdateUserProfileInfoAsync(UpdateProfileRequest request)
      {
         var url = $"{BaseUserUrl}/settings/profile";
         return await HttpService.Post<UpdateProfileResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UpdateContactInfoResponse)> UpdateUserContactInfoAsync(UpdateContactInfoRequest request)
      {
         var url = $"{BaseUserUrl}/settings/contact";
         return await HttpService.Post<UpdateContactInfoResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UpdatePrivacySettingResponse)> UpdateUserPrivacyAsync(UpdatePrivacySettingRequest request)
      {
         var url = $"{BaseUserUrl}/settings/privacy";
         return await HttpService.Post<UpdatePrivacySettingResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode HttpStatus, UpdateNotificationSettingResponse Response)> UpdateUserNotificationAsync(UpdateNotificationSettingRequest request)
      {
         var url = $"{BaseUserUrl}/settings/notification";
         return await HttpService.Post<UpdateNotificationSettingResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UpdateKeysResponse)> InsertUserX25519KeysAsync(UpdateKeysRequest request)
      {
         var url = $"{BaseUserUrl}/settings/keys/x25519";
         return await HttpService.Post<UpdateKeysResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UpdateKeysResponse)> InsertUserEd25519KeysAsync(UpdateKeysRequest request)
      {
         var url = $"{BaseUserUrl}/settings/keys/ed25519";
         return await HttpService.Post<UpdateKeysResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UserSentMessagesResponse)> GetUserSentMessagesAsync()
      {
         var url = $"{BaseUserUrl}/sent/messages";
         return await HttpService.Get<UserSentMessagesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserSentFilesResponse)> GetUserSentFilesAsync()
      {
         var url = $"{BaseUserUrl}/sent/files";
         return await HttpService.Get<UserSentFilesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserReceivedMessagesResponse)> GetUserReceivedMessagesAsync()
      {
         var url = $"{BaseUserUrl}/received/messages";
         return await HttpService.Get<UserReceivedMessagesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserReceivedFilesResponse)> GetUserReceivedFilesAsync()
      {
         var url = $"{BaseUserUrl}/received/files";
         return await HttpService.Get<UserReceivedFilesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserSearchResponse)> GetUserSearchResultsAsync(UserSearchParams searchInfo)
      {
         var url = $"{BaseUserUrl}/search/{searchInfo.Type}";
         var urlWithParams = url + "?value=" + searchInfo.Query + "&index=" + searchInfo.Index + "&count=" + searchInfo.Results;
         return await HttpService.Get<UserSearchResponse>(urlWithParams, true);
      }

      public async Task<(HttpStatusCode HttpStatus, UserEmailVerificationResponse Response)> VerifyUserEmailAddressAsync(VerifyUserEmailAddressRequest verificationInfo)
      {
         var url = $"{BaseUserUrl}/verify";
         return await HttpService.Post<UserEmailVerificationResponse>(url, verificationInfo, false);
      }
   }
}
