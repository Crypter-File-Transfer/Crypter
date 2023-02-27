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
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.Metrics;
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Common.Monads;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations
{
   public class CrypterApiClient : ICrypterApiClient
   {
      private readonly ICrypterHttpClient _crypterHttpClient;
      private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

      private EventHandler _refreshTokenRejectedHandler;

      public IFileTransferRequests FileTransfer { get; init; }
      public IMessageTransferRequests MessageTransfer { get; init; }
      public IUserAuthenticationRequests UserAuthentication { get; init; }
      public IUserConsentRequests UserConsent { get; init; }
      public IUserContactRequests UserContact { get; init; }

      public CrypterApiClient(HttpClient httpClient, ITokenRepository tokenRepository)
      {
         _crypterHttpClient = new CrypterHttpClient(httpClient);
         _crypterAuthenticatedHttpClient = new CrypterAuthenticatedHttpClient(httpClient, tokenRepository, this);

         FileTransfer = new FileTransferRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
         MessageTransfer = new MessageTransferRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
         UserAuthentication = new UserAuthenticationRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient, _refreshTokenRejectedHandler);
         UserConsent = new UserConsentRequests(_crypterAuthenticatedHttpClient);
         UserContact = new UserContactRequests(_crypterAuthenticatedHttpClient);
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

      #region File Transfer

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadAnonymousFilePreviewAsync(string hashId)
      {
         string url = "/file/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpClient.GetWithStatusCodeAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = "/file/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  _crypterHttpClient.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadUserFilePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = "/file/user/preview/?id={hashId}";
         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferFilePreviewResponse> data)>.FromRightAsync(
                  service.GetWithStatusCodeAsync<DownloadTransferFilePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = "/file/user/ciphertext/?id={hashId}";
         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

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
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<GetMasterKeyResponse>(url))
                from errorableResponse in ExtractErrorCode<GetMasterKeyError, GetMasterKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(InsertMasterKeyRequest request)
      {
         string url = "/keys/master";
         return from response in Either<InsertMasterKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, InsertMasterKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PutAsync<InsertMasterKeyRequest, InsertMasterKeyResponse>(url, request))
                from errorableResponse in ExtractErrorCode<InsertMasterKeyError, InsertMasterKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request)
      {
         string url = "/keys/master/recovery-proof";
         return from response in Either<GetMasterKeyRecoveryProofError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetMasterKeyRecoveryProofResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<GetMasterKeyRecoveryProofRequest, GetMasterKeyRecoveryProofResponse>(url, request))
                from errorableResponse in ExtractErrorCode<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync()
      {
         string url = "/keys/private";
         return from response in Either<GetPrivateKeyError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetPrivateKeyResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<GetPrivateKeyResponse>(url))
                from errorableResponse in ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(InsertKeyPairRequest request)
      {
         string url = "/keys/private";
         return from response in Either<InsertKeyPairError, (HttpStatusCode httpStatus, Either<ErrorResponse, InsertKeyPairResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PutAsync<InsertKeyPairRequest, InsertKeyPairResponse>(url, request))
                from errorableResponse in ExtractErrorCode<InsertKeyPairError, InsertKeyPairResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region User

      public Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserProfileAsync(string username, bool withAuthentication)
      {
         string url = "/user/{username}/profile";
         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         return from response in Either<GetUserProfileError, (HttpStatusCode httpStatus, Either<ErrorResponse, GetUserProfileResponse> data)>.FromRightAsync(
                  service.GetWithStatusCodeAsync<GetUserProfileResponse>(url))
                from errorableResponse in ExtractErrorCode<GetUserProfileError, GetUserProfileResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserReceivedFilesResponse>> GetReceivedFilesAsync()
      {
         string url = "/user/self/file/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserReceivedFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentFilesResponse>> GetSentFilesAsync()
      {
         string url = "/user/self/file/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentFilesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserSentFilesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentFilesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserReceivedMessagesResponse>> GetReceivedMessagesAsync()
      {
         string url = "/user/self/message/received";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserReceivedMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserReceivedMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserReceivedMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DummyError, UserSentMessagesResponse>> GetSentMessagesAsync()
      {
         string url = "/user/self/message/sent";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSentMessagesResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserSentMessagesResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSentMessagesResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Message Transfer

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadAnonymousMessagePreviewAsync(string hashId)
      {
         string url = "/message/preview/?id={hashId}";
         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  _crypterHttpClient.GetWithStatusCodeAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest)
      {
         string url = "/message/ciphertext/?id={hashId}";
         return from response in Either<DownloadTransferCiphertextError, (HttpStatusCode httpStatus, Either<ErrorResponse, StreamDownloadResponse> data)>.FromRightAsync(
                  _crypterHttpClient.PostWithStreamResponseAsync<DownloadTransferCiphertextRequest>(url, downloadRequest))
                from errorableResponse in ExtractErrorCode<DownloadTransferCiphertextError, StreamDownloadResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadUserMessagePreviewAsync(string hashId, bool withAuthentication)
      {
         string url = "/message/user/preview/?id={hashId}";
         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

         return from response in Either<DownloadTransferPreviewError, (HttpStatusCode httpStatus, Either<ErrorResponse, DownloadTransferMessagePreviewResponse> data)>.FromRightAsync(
                  service.GetWithStatusCodeAsync<DownloadTransferMessagePreviewResponse>(url))
                from errorableResponse in ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = "/message/user/ciphertext/?id={hashId}";
         ICrypterHttpClient service = withAuthentication
            ? _crypterAuthenticatedHttpClient
            : _crypterHttpClient;

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
                     _crypterHttpClient.GetWithStatusCodeAsync<DiskMetricsResponse>(url))
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
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserSearchResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSearchResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion

      #region Settings

      public Task<Either<DummyError, UserSettingsResponse>> GetUserSettingsAsync()
      {
         string url = "/settings";
         return from response in Either<DummyError, (HttpStatusCode httpStatus, Either<ErrorResponse, UserSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.GetWithStatusCodeAsync<UserSettingsResponse>(url))
                from errorableResponse in ExtractErrorCode<DummyError, UserSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateContactInfoAsync(UpdateContactInfoRequest request)
      {
         string url = "/settings/contact-info";
         return from response in Either<UpdateContactInfoError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateContactInfoResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<UpdateContactInfoRequest, UpdateContactInfoResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateContactInfoError, UpdateContactInfoResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateProfileInfoAsync(UpdateProfileRequest request)
      {
         string url = "/settings/profile";
         return from response in Either<UpdateProfileError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateProfileResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<UpdateProfileRequest, UpdateProfileResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateProfileError, UpdateProfileResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateNotificationPreferencesAsync(UpdateNotificationSettingsRequest request)
      {
         string url = "/settings/notification";
         return from response in Either<UpdateNotificationSettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdateNotificationSettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<UpdateNotificationSettingsRequest, UpdateNotificationSettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacySettingsAsync(UpdatePrivacySettingsRequest request)
      {
         string url = "/settings/privacy";
         return from response in Either<UpdatePrivacySettingsError, (HttpStatusCode httpStatus, Either<ErrorResponse, UpdatePrivacySettingsResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<UpdatePrivacySettingsRequest, UpdatePrivacySettingsResponse>(url, request))
                from errorableResponse in ExtractErrorCode<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>(response.data).AsTask()
                select errorableResponse;
      }

      public Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         string url = "/settings/verify";
         return from response in Either<VerifyEmailAddressError, (HttpStatusCode httpStatus, Either<ErrorResponse, VerifyEmailAddressResponse> data)>.FromRightAsync(
                  _crypterAuthenticatedHttpClient.PostWithStatusCodeAsync<VerifyEmailAddressRequest, VerifyEmailAddressResponse>(url, verificationInfo))
                from errorableResponse in ExtractErrorCode<VerifyEmailAddressError, VerifyEmailAddressResponse>(response.data).AsTask()
                select errorableResponse;
      }

      #endregion
   }
}
