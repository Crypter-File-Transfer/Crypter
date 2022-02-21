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

using Crypter.Common.FunctionalTypes;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.User.GetPublicProfile;
using Crypter.Contracts.Features.User.GetReceivedTransfers;
using Crypter.Contracts.Features.User.GetSentTransfers;
using Crypter.Contracts.Features.User.GetSettings;
using Crypter.Contracts.Features.User.Register;
using Crypter.Contracts.Features.User.Search;
using Crypter.Contracts.Features.User.UpdateContactInfo;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.Contracts.Features.User.UpdateNotificationSettings;
using Crypter.Contracts.Features.User.UpdatePrivacySettings;
using Crypter.Contracts.Features.User.UpdateProfile;
using Crypter.Contracts.Features.User.VerifyEmailAddress;
using Crypter.Web.Models;
using System.Threading.Tasks;

namespace Crypter.Web.Services.API
{
   public interface IUserApiService
   {
      Task<Either<ErrorResponse, UserRegisterResponse>> RegisterUserAsync(UserRegisterRequest registerRequest);
      Task<Either<ErrorResponse, GetUserPublicProfileResponse>> GetUserPublicProfileAsync(string username, bool withAuthentication);
      Task<Either<ErrorResponse, UserSettingsResponse>> GetUserSettingsAsync();
      Task<Either<ErrorResponse, UpdateProfileResponse>> UpdateUserProfileInfoAsync(UpdateProfileRequest request);
      Task<Either<ErrorResponse, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(UpdateContactInfoRequest request);
      Task<Either<ErrorResponse, UpdatePrivacySettingsResponse>> UpdateUserPrivacyAsync(UpdatePrivacySettingsRequest request);
      Task<Either<ErrorResponse, UpdateNotificationSettingsResponse>> UpdateUserNotificationAsync(UpdateNotificationSettingsRequest request);
      Task<Either<ErrorResponse, UpdateKeysResponse>> InsertUserX25519KeysAsync(UpdateKeysRequest request);
      Task<Either<ErrorResponse, UpdateKeysResponse>> InsertUserEd25519KeysAsync(UpdateKeysRequest request);
      Task<Either<ErrorResponse, UserSentMessagesResponse>> GetUserSentMessagesAsync();
      Task<Either<ErrorResponse, UserSentFilesResponse>> GetUserSentFilesAsync();
      Task<Either<ErrorResponse, UserReceivedMessagesResponse>> GetUserReceivedMessagesAsync();
      Task<Either<ErrorResponse, UserReceivedFilesResponse>> GetUserReceivedFilesAsync();
      Task<Either<ErrorResponse, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParams searchInfo);
      Task<Either<ErrorResponse, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo);
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

      public async Task<Either<ErrorResponse, UserRegisterResponse>> RegisterUserAsync(UserRegisterRequest registerRequest)
      {
         var url = $"{BaseUserUrl}/register";
         return await HttpService.PostAsync<UserRegisterResponse>(url, registerRequest, false);
      }

      public async Task<Either<ErrorResponse, GetUserPublicProfileResponse>> GetUserPublicProfileAsync(string username, bool withAuthentication)
      {
         var url = $"{BaseUserUrl}/{username}";
         return await HttpService.GetAsync<GetUserPublicProfileResponse>(url, withAuthentication);
      }

      public async Task<Either<ErrorResponse, UserSettingsResponse>> GetUserSettingsAsync()
      {
         var url = $"{BaseUserUrl}/settings";
         return await HttpService.GetAsync<UserSettingsResponse>(url, true);
      }

      public async Task<Either<ErrorResponse, UpdateProfileResponse>> UpdateUserProfileInfoAsync(UpdateProfileRequest request)
      {
         var url = $"{BaseUserUrl}/settings/profile";
         return await HttpService.PostAsync<UpdateProfileResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(UpdateContactInfoRequest request)
      {
         var url = $"{BaseUserUrl}/settings/contact";
         return await HttpService.PostAsync<UpdateContactInfoResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UpdatePrivacySettingsResponse>> UpdateUserPrivacyAsync(UpdatePrivacySettingsRequest request)
      {
         var url = $"{BaseUserUrl}/settings/privacy";
         return await HttpService.PostAsync<UpdatePrivacySettingsResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UpdateNotificationSettingsResponse>> UpdateUserNotificationAsync(UpdateNotificationSettingsRequest request)
      {
         var url = $"{BaseUserUrl}/settings/notification";
         return await HttpService.PostAsync<UpdateNotificationSettingsResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UpdateKeysResponse>> InsertUserX25519KeysAsync(UpdateKeysRequest request)
      {
         var url = $"{BaseUserUrl}/settings/keys/x25519";
         return await HttpService.PostAsync<UpdateKeysResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UpdateKeysResponse>> InsertUserEd25519KeysAsync(UpdateKeysRequest request)
      {
         var url = $"{BaseUserUrl}/settings/keys/ed25519";
         return await HttpService.PostAsync<UpdateKeysResponse>(url, request, true);
      }

      public async Task<Either<ErrorResponse, UserSentMessagesResponse>> GetUserSentMessagesAsync()
      {
         var url = $"{BaseUserUrl}/sent/messages";
         return await HttpService.GetAsync<UserSentMessagesResponse>(url, true);
      }

      public async Task<Either<ErrorResponse, UserSentFilesResponse>> GetUserSentFilesAsync()
      {
         var url = $"{BaseUserUrl}/sent/files";
         return await HttpService.GetAsync<UserSentFilesResponse>(url, true);
      }

      public async Task<Either<ErrorResponse, UserReceivedMessagesResponse>> GetUserReceivedMessagesAsync()
      {
         var url = $"{BaseUserUrl}/received/messages";
         return await HttpService.GetAsync<UserReceivedMessagesResponse>(url, true);
      }

      public async Task<Either<ErrorResponse, UserReceivedFilesResponse>> GetUserReceivedFilesAsync()
      {
         var url = $"{BaseUserUrl}/received/files";
         return await HttpService.GetAsync<UserReceivedFilesResponse>(url, true);
      }

      public async Task<Either<ErrorResponse, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParams searchInfo)
      {
         var url = $"{BaseUserUrl}/search/{searchInfo.Type}";
         var urlWithParams = url + "?value=" + searchInfo.Query + "&index=" + searchInfo.Index + "&count=" + searchInfo.Results;
         return await HttpService.GetAsync<UserSearchResponse>(urlWithParams, true);
      }

      public async Task<Either<ErrorResponse, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         var url = $"{BaseUserUrl}/verify";
         return await HttpService.PostAsync<VerifyEmailAddressResponse>(url, verificationInfo, false);
      }
   }
}
