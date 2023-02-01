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

using Crypter.Common.Client.Implementations.Requests;
using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Authentication;
using Crypter.Common.Contracts.Features.Consent;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.Metrics;
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Common.Monads;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations
{
   public class CrypterApiService : ICrypterApiService
   {
      private readonly ICrypterHttpService _crypterHttpService;
      private readonly ICrypterAuthenticatedHttpService _crypterAuthenticatedHttpService;

      private EventHandler _refreshTokenRejectedHandler;

      public IFileTransferRequests FileTransfer { get; init; }

      public CrypterApiService(ICrypterHttpService crypterHttpService, ICrypterAuthenticatedHttpService crypterAuthenticatedHttpService)
      {
         _crypterHttpService = crypterHttpService;
         _crypterAuthenticatedHttpService = crypterAuthenticatedHttpService;

         FileTransfer = new FileTransferRequests(_crypterHttpService, _crypterAuthenticatedHttpService);
      }

      /// <summary>
      /// Lift the first error code out of the API error response.
      /// </summary>
      /// <typeparam name="TErrorCode"></typeparam>
      /// <typeparam name="TResponse"></typeparam>
      /// <param name="response"></param>
      /// <returns></returns>
      /// <remarks>
      /// Need to refactor Crypter.Web and other client services to handle multiple error codes.
      /// </remarks>
      private static Either<TErrorCode, TResponse> ExtractErrorCode<TErrorCode, TResponse>(Either<ErrorResponse, TResponse> response)
      {
         return response
            .BindLeft<TErrorCode>(x => x.Errors.Select(x => (TErrorCode)(object)x.ErrorCode).First());
      }

      public event EventHandler RefreshTokenRejectedEventHandler
      {
         add => _refreshTokenRejectedHandler = (EventHandler)Delegate.Combine(_refreshTokenRejectedHandler, value);
         remove => _refreshTokenRejectedHandler = (EventHandler)Delegate.Remove(_refreshTokenRejectedHandler, value);
      }

      #region Authentication

      public Task<Either<RegistrationError, RegistrationResponse>> RegisterUserAsync(RegistrationRequest registerRequest)
      {
         string url = "/authentication/register";
         return from response in Either<RegistrationError, (HttpStatusCode httpStatus, Either<ErrorResponse, RegistrationResponse> data)>.FromRightAsync(
                     _crypterHttpService.PostAsync<RegistrationRequest, RegistrationResponse>(url, registerRequest))
                from errorableResponse in ExtractErrorCode<RegistrationError, RegistrationResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest)
      {
         string url = "/authentication/login";
         return from response in Either<LoginError, (HttpStatusCode httpStatus, Either<ErrorResponse, LoginResponse> data)>.FromRightAsync(
                     _crypterHttpService.PostAsync<LoginRequest, LoginResponse>(url, loginRequest))
                from errorableResponse in ExtractErrorCode<LoginError, LoginResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<TestPasswordError, TestPasswordResponse>> TestPasswordAsync(TestPasswordRequest testPasswordRequest)
      {
         string url = "/authentication/password/test";
         return from response in Either<TestPasswordError, (HttpStatusCode httpStatus, Either<ErrorResponse, TestPasswordResponse> data)>.FromRightAsync(
                     _crypterAuthenticatedHttpService.PostAsync<TestPasswordRequest, TestPasswordResponse>(url, testPasswordRequest))
                from errorableResponse in ExtractErrorCode<TestPasswordError, TestPasswordResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public async Task<Either<RefreshError, RefreshResponse>> RefreshAsync()
      {
         string url = "/authentication/refresh";
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
         string url = "/authentication/logout";
         return from response in Either<LogoutError, (HttpStatusCode httpStatus, Either<ErrorResponse, LogoutResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<LogoutResponse>(url, true))
                from errorableResponse in ExtractErrorCode<LogoutError, LogoutResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Consent

      public Task<Either<DummyError, ConsentToRecoveryKeyRisksResponse>> ConsentToRecoveryKeyRisksAsync()
      {
         string url = "/consent/recovery-key";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, ConsentToRecoveryKeyRisksResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<ConsentToRecoveryKeyRisksResponse>(url, true))
                from errorableResponse in ExtractErrorCode<DummyError, ConsentToRecoveryKeyRisksResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Contacts

      public Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync()
      {
         string url = "/contacts";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetUserContactsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<GetUserContactsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, GetUserContactsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request)
      {
         string url = "/contacts";
         return from response in Either<AddUserContactError, (HttpStatusCode httpStatus, Either<ErrorResponse, AddUserContactResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<AddUserContactRequest, AddUserContactResponse>(url, request))
                from errorableResponse in ExtractErrorCode<AddUserContactError, AddUserContactResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, RemoveContactResponse>> RemoveUserContactAsync(RemoveContactRequest request)
      {
         string url = "/contacts";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, RemoveContactResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.DeleteAsync<RemoveContactRequest, RemoveContactResponse>(url, request))
                from errorableResponse in ExtractErrorCode<DummyError, RemoveContactResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region File Transfer

      public Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication)
      {
         string url = "/file";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
         using var content = new MultipartFormDataContent
         {
            { new StringContent(JsonSerializer.Serialize(uploadRequest), Encoding.UTF8, "application/json"), "Data" },
            { new StreamContent(encryptionStream), "Ciphertext", "Ciphertext" }
         };
         request.Content = content;

         return from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                     service.SendAsync<UploadTransferResponse>(request))
                from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadAnonymousFilePreviewAsync(string hashId)
      {
         string url = "/file/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpService.GetAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = "/file/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  _crypterHttpService.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadUserFilePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = "/file/user/preview/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  service.GetAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = "/file/user/ciphertext/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  service.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Keys

      public Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync()
      {
         string url = "/keys/master";
         return from response in Either<GetMasterKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetMasterKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<GetMasterKeyResponse>(url))
                from errorableResponse in ExtractErrorCode<GetMasterKeyError, GetMasterKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(InsertMasterKeyRequest request)
      {
         string url = "/keys/master";
         return from response in Either<InsertMasterKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, InsertMasterKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PutAsync<InsertMasterKeyRequest, InsertMasterKeyResponse>(url, request))
                from errorableResponse in ExtractErrorCode<InsertMasterKeyError, InsertMasterKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request)
      {
         string url = "/keys/master/recovery-proof";
         return from response in Either<GetMasterKeyRecoveryProofError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetMasterKeyRecoveryProofResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<GetMasterKeyRecoveryProofRequest, GetMasterKeyRecoveryProofResponse>(url, request))
                from errorableResponse in ExtractErrorCode<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync()
      {
         string url = "/keys/private";
         return from response in Either<GetPrivateKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetPrivateKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<GetPrivateKeyResponse>(url))
                from errorableResponse in ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(InsertKeyPairRequest request)
      {
         string url = "/keys/private";
         return from response in Either<InsertKeyPairError, (HttpStatusCode httpStatus, Either<ErrorResponse, InsertKeyPairResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PutAsync<InsertKeyPairRequest, InsertKeyPairResponse>(url, request))
                from errorableResponse in ExtractErrorCode<InsertKeyPairError, InsertKeyPairResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region User

      public Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserProfileAsync(string username, bool withAuthentication)
      {
         string url = "/user/{username}/profile";
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
         string url = "/user/self/file/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserReceivedFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentFilesResponse>> GetSentFilesAsync()
      {
         string url = "/user/self/file/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSentFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserReceivedMessagesResponse>> GetReceivedMessagesAsync()
      {
         string url = "/user/self/message/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserReceivedMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentMessagesResponse>> GetSentMessagesAsync()
      {
         string url = "/user/self/message/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSentMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> SendUserFileTransferAsync(string username, UploadFileTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication)
      {
         string url = "/user/{username}/file";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
         var content = new MultipartFormDataContent
         {
            { new StringContent(JsonSerializer.Serialize(uploadRequest), Encoding.UTF8, "application/json"), "Data" },
            { new StreamContent(encryptionStream), "Ciphertext", "Ciphertext" }
         };
         request.Content = content;

         var result = from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                        service.SendAsync<UploadTransferResponse>(request))
                      from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                      select errorableResponse;
         await result;

         request.Dispose();
         content.Dispose();

         return result.Result;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> SendUserMessageTransferAsync(string username, UploadMessageTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication)
      {
         string url = "/user/{username}/message";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
         var content = new MultipartFormDataContent
         {
            { new StringContent(JsonSerializer.Serialize(uploadRequest), Encoding.UTF8, "application/json"), "Data" },
            { new StreamContent(encryptionStream), "Ciphertext", "Ciphertext" }
         };
         request.Content = content;

         var result = from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                        service.SendAsync<UploadTransferResponse>(request))
                      from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                      select errorableResponse;
         await result;

         request.Dispose();
         content.Dispose();

         return result.Result;
      }

      #endregion

      #region Message Transfer

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication)
      {
         string url = "/message";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
         var content = new MultipartFormDataContent
         {
            { new StringContent(JsonSerializer.Serialize(uploadRequest), Encoding.UTF8, "application/json"), "Data" },
            { new StreamContent(encryptionStream), "Ciphertext", "Ciphertext" }
         };
         request.Content = content;

         var result = from response in Either<UploadTransferError, (HttpStatusCode httpStatus, Either<ErrorResponse, UploadTransferResponse> data)>.FromRightAsync(
                        service.SendAsync<UploadTransferResponse>(request))
                      from errorableResponse in ExtractErrorCode<UploadTransferError, UploadTransferResponse>(response.data).AsTask()
                      select errorableResponse;
         await result;

         request.Dispose();
         content.Dispose();

         return result.Result;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadAnonymousMessagePreviewAsync(string hashId)
      {
         string url = "/message/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpService.GetAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = "/message/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  _crypterHttpService.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadUserMessagePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = "/message/user/preview/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  service.GetAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = "/message/user/ciphertext/?id={hashId}";
         ICrypterHttpService service = withAuthentication
            ? _crypterAuthenticatedHttpService
            : _crypterHttpService;

         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  service.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Metrics

      public Task<Either<DummyError, DiskMetricsResponse>> GetDiskMetricsAsync()
      {
         string url = "/metrics/disk";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, DiskMetricsResponse> data)>.FromRightAsync(
                     _crypterHttpService.GetAsync<DiskMetricsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, DiskMetricsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Search

      public Task<Either<DummyError, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParameters searchInfo)
      {
         StringBuilder urlBuilder = new StringBuilder("/search/user?value=");
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
         string url = "/settings";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.GetAsync<UserSettingsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateContactInfoAsync(UpdateContactInfoRequest request)
      {
         string url = "/settings/contact-info";
         return from response in Either<UpdateContactInfoError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateContactInfoResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateContactInfoRequest, UpdateContactInfoResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateContactInfoError, UpdateContactInfoResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateProfileInfoAsync(UpdateProfileRequest request)
      {
         string url = "/settings/profile";
         return from response in Either<UpdateProfileError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateProfileResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateProfileRequest, UpdateProfileResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateProfileError, UpdateProfileResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateNotificationPreferencesAsync(UpdateNotificationSettingsRequest request)
      {
         string url = "/settings/notification";
         return from response in Either<UpdateNotificationSettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateNotificationSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdateNotificationSettingsRequest, UpdateNotificationSettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacySettingsAsync(UpdatePrivacySettingsRequest request)
      {
         string url = "/settings/privacy";
         return from response in Either<UpdatePrivacySettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdatePrivacySettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<UpdatePrivacySettingsRequest, UpdatePrivacySettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         string url = "/settings/verify";
         return from response in Either<VerifyEmailAddressError, (HttpStatusCode httpStatus, Either<ErrorResponse, VerifyEmailAddressResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpService.PostAsync<VerifyEmailAddressRequest, VerifyEmailAddressResponse>(url, verificationInfo))
                from errorableResponse in ExtractErrorCode<VerifyEmailAddressError, VerifyEmailAddressResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion
   }
}
