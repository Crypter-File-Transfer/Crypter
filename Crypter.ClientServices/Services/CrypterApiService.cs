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
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Contacts;
using Crypter.Contracts.Features.Keys;
using Crypter.Contracts.Features.Metrics;
using Crypter.Contracts.Features.Settings;
using Crypter.Contracts.Features.Transfer;
using Crypter.Contracts.Features.Users;
using Crypter.Contracts.Features.Users.GetReceivedTransfers;
using Crypter.Contracts.Features.Users.GetSentTransfers;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class CrypterApiService : ICrypterApiService
   {
      private readonly ICrypterHttpService _crypterHttpService;
      private readonly ICrypterAuthenticatedHttpService _crypterAuthenticatedHttpService;

      private readonly string _baseApiUrl;
      private EventHandler _refreshTokenRejectedHandler;

      public CrypterApiService(ICrypterHttpService crypterHttpService, ICrypterAuthenticatedHttpService crypterAuthenticatedHttpService, IClientApiSettings clientApiSettings)
      {
         _crypterHttpService = crypterHttpService;
         _crypterAuthenticatedHttpService = crypterAuthenticatedHttpService;
         _baseApiUrl = clientApiSettings.ApiBaseUrl;
      }

      private static Either<TErrorCode, TResponse> ExtractErrorCode<TErrorCode, TResponse>(Either<ErrorResponse, TResponse> response)
      {
         return response.Match(
            left: left => (TErrorCode)(object)left.ErrorCode,
            right: right => right,
            neither: Either<TErrorCode, TResponse>.Neither);
      }

      public event EventHandler RefreshTokenRejectedEventHandler
      {
         add => _refreshTokenRejectedHandler = (EventHandler)Delegate.Combine(_refreshTokenRejectedHandler, value);
         remove => _refreshTokenRejectedHandler = (EventHandler)Delegate.Remove(_refreshTokenRejectedHandler, value);
      }

      #region Authentication

      public Task<Either<RegistrationError, RegistrationResponse>> RegisterUserAsync(RegistrationRequest registerRequest)
      {
         string url = $"{_baseApiUrl}/authentication/register";
         return from response in Either<RegistrationError, (HttpStatusCode httpStatus, Either<ErrorResponse, RegistrationResponse> data)>.FromRightAsync(
                     _crypterHttpService.PostAsync<RegistrationRequest, RegistrationResponse>(url, registerRequest))
                from errorableResponse in ExtractErrorCode<RegistrationError, RegistrationResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest)
      {
         string url = $"{_baseApiUrl}/authentication/login";
         return from response in Either<LoginError, (HttpStatusCode httpStatus, Either<ErrorResponse, LoginResponse> data)>.FromRightAsync(
                     _crypterHttpService.PostAsync<LoginRequest, LoginResponse>(url, loginRequest))
                from errorableResponse in ExtractErrorCode<LoginError, LoginResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public async Task<Either<RefreshError, RefreshResponse>> RefreshAsync()
      {
         string url = $"{_baseApiUrl}/authentication/refresh";
         var task = from response in Either<RefreshError, (HttpStatusCode httpStatus, Either<ErrorResponse, RefreshResponse> data)>.FromRightAsync(
                        _crypterAuthenticatedHttpService.GetAsync<RefreshResponse>(url, true))
                    from errorableResponse in ExtractErrorCode<RefreshError, RefreshResponse>(response.data).AsTask()
                    select errorableResponse;

         var apiResponse = await task;
         apiResponse.DoLeftOrNeither(() => _refreshTokenRejectedHandler?.Invoke(this, EventArgs.Empty));
         return apiResponse;
      }

      public Task<Either<LogoutError, LogoutResponse>> LogoutAsync()
      {
         string url = $"{_baseApiUrl}/authentication/logout";
         return from response in Either<LogoutError, (HttpStatusCode httpStatus, Either<ErrorResponse, LogoutResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<LogoutResponse>(url, true))
                from errorableResponse in ExtractErrorCode<LogoutError, LogoutResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Contacts

      public Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync()
      {
         string url = $"{_baseApiUrl}/contacts";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetUserContactsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<GetUserContactsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, GetUserContactsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request)
      {
         string url = $"{_baseApiUrl}/contacts";
         return from response in Either<AddUserContactError, (HttpStatusCode httpStatus, Either<ErrorResponse, AddUserContactResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<AddUserContactRequest, AddUserContactResponse>(url, request))
                from errorableResponse in ExtractErrorCode<AddUserContactError, AddUserContactResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, RemoveUserContactResponse>> RemoveUserContactAsync(RemoveUserContactRequest request)
      {
         string url = $"{_baseApiUrl}/contacts";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, RemoveUserContactResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.DeleteAsync<RemoveUserContactRequest, RemoveUserContactResponse>(url, request))
                from errorableResponse in ExtractErrorCode<DummyError, RemoveUserContactResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region File Transfer

      public Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/file";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                  service.PostAsync<UploadFileTransferRequest, UploadTransferResponse>(url, uploadRequest))
                from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadAnonymousFilePreviewAsync(string hashId)
      {
         string url = $"{_baseApiUrl}/file/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpService.GetAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = $"{_baseApiUrl}/file/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferCiphertextResponse> data)>.FromRightAsync(
                  _crypterHttpService.PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadUserFilePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/file/user/preview/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  service.GetAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/file/user/ciphertext/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferCiphertextResponse> data)>.FromRightAsync(
                  service.PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Keys

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync()
      {
         string url = $"{_baseApiUrl}/keys/diffie-hellman/private";
         return from response in Either<GetPrivateKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetPrivateKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<GetPrivateKeyResponse>(url))
                from errorableResponse in ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(InsertKeyPairRequest request)
      {
         string url = $"{_baseApiUrl}/keys/diffie-hellman";
         return from response in Either<InsertKeyPairError, (HttpStatusCode httpStatus, Either<ErrorResponse, InsertKeyPairResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PutAsync<InsertKeyPairRequest, InsertKeyPairResponse>(url, request))
                from errorableResponse in ExtractErrorCode<InsertKeyPairError, InsertKeyPairResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region User

      public Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserProfileAsync(string username, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/user/{username}/profile";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<GetUserProfileError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetUserProfileResponse> data)>.FromRightAsync(
                  service.GetAsync<GetUserProfileResponse>(url))
                from errorableResponse in ExtractErrorCode<GetUserProfileError, GetUserProfileResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserReceivedFilesResponse>> GetReceivedFilesAsync()
      {
         string url = $"{_baseApiUrl}/user/self/file/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserReceivedFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentFilesResponse>> GetSentFilesAsync()
      {
         string url = $"{_baseApiUrl}/user/self/file/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSentFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserReceivedMessagesResponse>> GetReceivedMessagesAsync()
      {
         string url = $"{_baseApiUrl}/user/self/message/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserReceivedMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentMessagesResponse>> GetSentMessagesAsync()
      {
         string url = $"{_baseApiUrl}/user/self/message/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSentMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UploadTransferError, UploadTransferResponse>> SendUserFileTransferAsync(string username, UploadFileTransferRequest uploadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/user/{username}/file";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                  service.PostAsync<UploadFileTransferRequest, UploadTransferResponse>(url, uploadRequest))
                from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UploadTransferError, UploadTransferResponse>> SendUserMessageTransferAsync(string username, UploadMessageTransferRequest uploadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/user/{username}/message";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                  service.PostAsync<UploadMessageTransferRequest, UploadTransferResponse>(url, uploadRequest))
                from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Message Transfer

      public Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/message";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                  service.PostAsync<UploadMessageTransferRequest, UploadTransferResponse>(url, uploadRequest))
                from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadAnonymousMessagePreviewAsync(string hashId)
      {
         string url = $"{_baseApiUrl}/message/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpService.GetAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = $"{_baseApiUrl}/message/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferCiphertextResponse> data)>.FromRightAsync(
                  _crypterHttpService.PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadUserMessagePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/message/user/preview/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  service.GetAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseApiUrl}/message/user/ciphertext/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferCiphertextResponse> data)>.FromRightAsync(
                  service.PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Metrics

      public Task<Either<DummyError, DiskMetricsResponse>> GetDiskMetricsAsync()
      {
         string url = $"{_baseApiUrl}/metrics/disk";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, DiskMetricsResponse> data)>.FromRightAsync(
                     _crypterHttpService.GetAsync<DiskMetricsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, DiskMetricsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Search

      public Task<Either<DummyError, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParameters searchInfo)
      {
         StringBuilder urlBuilder = new StringBuilder($"{_baseApiUrl}/search/user?value=");
         urlBuilder.Append(searchInfo.Keyword);
         urlBuilder.Append("&index=");
         urlBuilder.Append(searchInfo.Index);
         urlBuilder.Append("&count=");
         urlBuilder.Append(searchInfo.Count);

         string url = urlBuilder.ToString();

         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSearchResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSearchResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSearchResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Settings

      public Task<Either<DummyError, UserSettingsResponse>> GetUserSettingsAsync()
      {
         string url = $"{_baseApiUrl}/settings";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSettingsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateContactInfoAsync(UpdateContactInfoRequest request)
      {
         string url = $"{_baseApiUrl}/settings/contact-info";
         return from response in Either<UpdateContactInfoError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateContactInfoResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateContactInfoRequest, UpdateContactInfoResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateContactInfoError, UpdateContactInfoResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateProfileInfoAsync(UpdateProfileRequest request)
      {
         string url = $"{_baseApiUrl}/settings/profile";
         return from response in Either<UpdateProfileError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateProfileResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateProfileRequest, UpdateProfileResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateProfileError, UpdateProfileResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateNotificationPreferencesAsync(UpdateNotificationSettingsRequest request)
      {
         string url = $"{_baseApiUrl}/settings/notification";
         return from response in Either<UpdateNotificationSettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateNotificationSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateNotificationSettingsRequest, UpdateNotificationSettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacySettingsAsync(UpdatePrivacySettingsRequest request)
      {
         string url = $"{_baseApiUrl}/settings/privacy";
         return from response in Either<UpdatePrivacySettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdatePrivacySettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdatePrivacySettingsRequest, UpdatePrivacySettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         string url = $"{_baseApiUrl}/settings/verify";
         return from response in Either<VerifyEmailAddressError, (HttpStatusCode httpStatus, Either<ErrorResponse, VerifyEmailAddressResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<VerifyEmailAddressRequest, VerifyEmailAddressResponse>(url, verificationInfo))
                from errorableResponse in ExtractErrorCode<VerifyEmailAddressError, VerifyEmailAddressResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion
   }
}
