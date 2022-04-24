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
using Crypter.ClientServices.Implementations.Extensions;
using Crypter.ClientServices.Interfaces;
using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using Crypter.Contracts.Features.Authentication.Refresh;
using Crypter.Contracts.Features.Metrics.Disk;
using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
using Crypter.Contracts.Features.Transfer.Upload;
using Crypter.Contracts.Features.User.AddContact;
using Crypter.Contracts.Features.User.GetContacts;
using Crypter.Contracts.Features.User.GetPrivateKey;
using Crypter.Contracts.Features.User.GetPublicProfile;
using Crypter.Contracts.Features.User.GetReceivedTransfers;
using Crypter.Contracts.Features.User.GetSentTransfers;
using Crypter.Contracts.Features.User.GetSettings;
using Crypter.Contracts.Features.User.Register;
using Crypter.Contracts.Features.User.RemoveUserContact;
using Crypter.Contracts.Features.User.Search;
using Crypter.Contracts.Features.User.UpdateContactInfo;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.Contracts.Features.User.UpdateNotificationSettings;
using Crypter.Contracts.Features.User.UpdatePrivacySettings;
using Crypter.Contracts.Features.User.UpdateProfile;
using Crypter.Contracts.Features.User.VerifyEmailAddress;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Implementations
{
   public class CrypterApiService : CrypterApiHttpService, ICrypterApiService
   {
      private readonly ITokenRepository _tokenRepository;

      private readonly string _baseAuthenticationUrl;
      private readonly string _baseMetricsUrl;
      private readonly string _baseUserUrl;
      private readonly string _baseTransferUrl;

      private readonly SemaphoreSlim _refreshSemaphore = new(1);

      public CrypterApiService(HttpClient httpClient, ITokenRepository tokenRepository, IClientApiSettings clientApiSettings)
         : base(httpClient)
      {
         _tokenRepository = tokenRepository;
         _baseAuthenticationUrl = $"{clientApiSettings.ApiBaseUrl}/authentication";
         _baseMetricsUrl = $"{clientApiSettings.ApiBaseUrl}/metrics";
         _baseUserUrl = $"{clientApiSettings.ApiBaseUrl}/user";
         _baseTransferUrl = $"{clientApiSettings.ApiBaseUrl}/transfer";
      }

      private async Task<Maybe<TokenObject>> GetAuthenticationTokenAsync()
         => await _tokenRepository.GetAuthenticationTokenAsync();

      private async Task<Maybe<TokenObject>> GetRefreshTokenAsync()
         => await _tokenRepository.GetRefreshTokenAsync();

      private async Task<(HttpStatusCode httpStatus, TResponse response)> UseAuthenticationMiddleware<TResponse>(Func<string, Task<(HttpStatusCode httpStatus, TResponse response)>> request)
      {
         async Task<(HttpStatusCode httpStatus, TResponse response)> MakeRequestAsync()
         {
            var maybeAuthenticationToken = await GetAuthenticationTokenAsync();
            return await maybeAuthenticationToken.MatchAsync(
               () => (HttpStatusCode.Unauthorized, default),
               async tokenObject => await request(tokenObject.Token));
         }

         _refreshSemaphore.Wait();
         _refreshSemaphore.Release();
         var initialAttempt = await MakeRequestAsync();
         if (initialAttempt.httpStatus == HttpStatusCode.Unauthorized)
         {
            await _refreshSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
               var maybeResponse = await RefreshAsync();
               await maybeResponse.DoRightAsync(async response =>
               {
                  await _tokenRepository.StoreAuthenticationTokenAsync(response.AuthenticationToken);
                  await _tokenRepository.StoreRefreshTokenAsync(response.RefreshToken, response.RefreshTokenType);
               });

               return await maybeResponse.MatchAsync(
                  _ => initialAttempt,
                  async _ => await MakeRequestAsync());
            }
            finally
            {
               _refreshSemaphore.Release();
            }
         }
         else
         {
            return initialAttempt;
         }
      }

      public async Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest)
      {
         string url = $"{_baseAuthenticationUrl}/login";
         var (_, response) = await PostAsync<LoginRequest, LoginResponse>(url, loginRequest, Maybe<string>.None);

         return response.ExtractErrorCode<LoginError, LoginResponse>();
      }

      public async Task<Either<RefreshError, RefreshResponse>> RefreshAsync()
      {
         var maybeRefreshToken = await GetRefreshTokenAsync();
         return await maybeRefreshToken.MatchAsync(
            () => new(),
            async refreshToken =>
            {
               string url = $"{_baseAuthenticationUrl}/refresh";
               var (_, response) = await GetAsync<RefreshResponse>(url, refreshToken.Token);
               return response.ExtractErrorCode<RefreshError, RefreshResponse>();
            });
      }

      public async Task<Either<LogoutError, LogoutResponse>> LogoutAsync(LogoutRequest logoutRequest)
      {
         var maybeRefreshToken = await GetRefreshTokenAsync();
         return await maybeRefreshToken.MatchAsync(
            () => new(),
            async refreshToken =>
            {
               string url = $"{_baseAuthenticationUrl}/logout";
               var (_, response) = await PostAsync<LogoutRequest, LogoutResponse>(url, logoutRequest, refreshToken.Token);
               return response.ExtractErrorCode<LogoutError, LogoutResponse>();
            });
      }

      public async Task<Either<DummyError, DiskMetricsResponse>> GetDiskMetricsAsync()
      {
         string url = $"{_baseMetricsUrl}/disk";
         var (_, response) = await GetAsync<DiskMetricsResponse>(url, Maybe<string>.None);

         return response.ExtractErrorCode<DummyError, DiskMetricsResponse>();
      }

      public async Task<Either<UserRegisterError, UserRegisterResponse>> RegisterUserAsync(UserRegisterRequest registerRequest)
      {
         string url = $"{_baseUserUrl}/register";
         var (_, response) = await PostAsync<UserRegisterRequest, UserRegisterResponse>(url, registerRequest, Maybe<string>.None);

         return response.ExtractErrorCode<UserRegisterError, UserRegisterResponse>();
      }

      public async Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserPublicProfileAsync(string username, bool withAuthentication)
      {
         string url = $"{_baseUserUrl}/profile/{username}";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await GetAsync<GetUserProfileResponse>(url, token))
            : await GetAsync<GetUserProfileResponse>(url, Maybe<string>.None);

         return response.ExtractErrorCode<GetUserProfileError, GetUserProfileResponse>();
      }

      public async Task<Either<DummyError, UserSettingsResponse>> GetUserSettingsAsync()
      {
         string url = $"{_baseUserUrl}/settings";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserSettingsResponse>(url, token));

         return response.ExtractErrorCode<DummyError, UserSettingsResponse>();
      }

      public async Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateUserProfileInfoAsync(UpdateProfileRequest request)
      {
         string url = $"{_baseUserUrl}/settings/profile";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PostAsync<UpdateProfileRequest, UpdateProfileResponse>(url, request, token));

         return response.ExtractErrorCode<UpdateProfileError, UpdateProfileResponse>();
      }

      public async Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(UpdateContactInfoRequest request)
      {
         string url = $"{_baseUserUrl}/settings/contact";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PostAsync<UpdateContactInfoRequest, UpdateContactInfoResponse>(url, request, token));

         return response.ExtractErrorCode<UpdateContactInfoError, UpdateContactInfoResponse>();
      }

      public async Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacyAsync(UpdatePrivacySettingsRequest request)
      {
         string url = $"{_baseUserUrl}/settings/privacy";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PostAsync<UpdatePrivacySettingsRequest, UpdatePrivacySettingsResponse>(url, request, token));

         return response.ExtractErrorCode<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>();
      }

      public async Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateUserNotificationAsync(UpdateNotificationSettingsRequest request)
      {
         string url = $"{_baseUserUrl}/settings/notification";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PostAsync<UpdateNotificationSettingsRequest, UpdateNotificationSettingsResponse>(url, request, token));

         return response.ExtractErrorCode<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>();
      }

      public async Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetUserX25519PrivateKeyAsync()
      {
         string url = $"{_baseUserUrl}/settings/keys/x25519/private";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<GetPrivateKeyResponse>(url, token));

         return response.ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>();
      }

      public async Task<Either<UpdateKeysError, UpdateKeysResponse>> InsertUserX25519KeysAsync(UpdateKeysRequest request)
      {
         string url = $"{_baseUserUrl}/settings/keys/x25519";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PutAsync<UpdateKeysRequest, UpdateKeysResponse>(url, request, token));

         return response.ExtractErrorCode<UpdateKeysError, UpdateKeysResponse>();
      }

      public async Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetUserEd25519PrivateKeyAsync()
      {
         string url = $"{_baseUserUrl}/settings/keys/ed25519/private";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<GetPrivateKeyResponse>(url, token));

         return response.ExtractErrorCode<GetPrivateKeyError, GetPrivateKeyResponse>();
      }

      public async Task<Either<UpdateKeysError, UpdateKeysResponse>> InsertUserEd25519KeysAsync(UpdateKeysRequest request)
      {
         string url = $"{_baseUserUrl}/settings/keys/ed25519";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PutAsync<UpdateKeysRequest, UpdateKeysResponse>(url, request, token));

         return response.ExtractErrorCode<UpdateKeysError, UpdateKeysResponse>();
      }

      public async Task<Either<DummyError, UserSentMessagesResponse>> GetUserSentMessagesAsync()
      {
         string url = $"{_baseUserUrl}/sent/messages";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserSentMessagesResponse>(url, token));

         return response.ExtractErrorCode<DummyError, UserSentMessagesResponse>();
      }

      public async Task<Either<DummyError, UserSentFilesResponse>> GetUserSentFilesAsync()
      {
         string url = $"{_baseUserUrl}/sent/files";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserSentFilesResponse>(url, token));

         return response.ExtractErrorCode<DummyError, UserSentFilesResponse>();
      }

      public async Task<Either<DummyError, UserReceivedMessagesResponse>> GetUserReceivedMessagesAsync()
      {
         string url = $"{_baseUserUrl}/received/messages";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserReceivedMessagesResponse>(url, token));

         return response.ExtractErrorCode<DummyError, UserReceivedMessagesResponse>();
      }

      public async Task<Either<DummyError, UserReceivedFilesResponse>> GetUserReceivedFilesAsync()
      {
         string url = $"{_baseUserUrl}/received/files";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserReceivedFilesResponse>(url, token));

         return response.ExtractErrorCode<DummyError, UserReceivedFilesResponse>();
      }

      public async Task<Either<DummyError, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParameters searchInfo)
      {
         StringBuilder urlBuilder = new($"{_baseUserUrl}/search");
         urlBuilder.Append($"?value={searchInfo.Keyword}");
         urlBuilder.Append($"&index={searchInfo.Index}");
         urlBuilder.Append($"&count={searchInfo.Count}");
         string url = urlBuilder.ToString();

         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<UserSearchResponse>(url, token));
         return response.ExtractErrorCode<DummyError, UserSearchResponse>();
      }

      public async Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         string url = $"{_baseUserUrl}/verify";
         var (_, response) = await PostAsync<VerifyEmailAddressRequest, VerifyEmailAddressResponse>(url, verificationInfo, Maybe<string>.None);

         return response.ExtractErrorCode<VerifyEmailAddressError, VerifyEmailAddressResponse>();
      }

      public async Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync()
      {
         string url = $"{_baseUserUrl}/contacts";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await GetAsync<GetUserContactsResponse>(url, token));

         return response.ExtractErrorCode<DummyError, GetUserContactsResponse>();
      }

      public async Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request)
      {
         string url = $"{_baseUserUrl}/contacts";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await PostAsync<AddUserContactRequest, AddUserContactResponse>(url, request, token));

         return response.ExtractErrorCode<AddUserContactError, AddUserContactResponse>();
      }

      public async Task<Either<DummyError, RemoveUserContactResponse>> RemoveUserContactAsync(RemoveUserContactRequest request)
      {
         string url = $"{_baseUserUrl}/contacts";
         var (_, response) = await UseAuthenticationMiddleware(async (token) => await DeleteAsync<RemoveUserContactRequest, RemoveUserContactResponse>(url, request, token));

         return response.ExtractErrorCode<DummyError, RemoveUserContactResponse>();
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, Maybe<string> recipient, bool withAuthentication)
      {
         string url = recipient.Match(
            () => $"{_baseTransferUrl}/message",
            x => $"{_baseTransferUrl}/message/{x}");

         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<UploadMessageTransferRequest, UploadTransferResponse>(url, uploadRequest, token))
            : await PostAsync<UploadMessageTransferRequest, UploadTransferResponse>(url, uploadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<UploadTransferError, UploadTransferResponse>();
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, Maybe<string> recipient, bool withAuthentication)
      {
         string url = recipient.Match(
            () => $"{_baseTransferUrl}/file",
            x => $"{_baseTransferUrl}/file/{x}");

         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<UploadFileTransferRequest, UploadTransferResponse>(url, uploadRequest, token))
            : await PostAsync<UploadFileTransferRequest, UploadTransferResponse>(url, uploadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<UploadTransferError, UploadTransferResponse>();
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadMessagePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/message/preview";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferPreviewRequest, DownloadTransferMessagePreviewResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferPreviewRequest, DownloadTransferMessagePreviewResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>();
      }

      public async Task<Either<DownloadTransferSignatureError, DownloadTransferSignatureResponse>> DownloadMessageSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/message/signature";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferSignatureRequest, DownloadTransferSignatureResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferSignatureRequest, DownloadTransferSignatureResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferSignatureError, DownloadTransferSignatureResponse>();
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadMessageCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/message/ciphertext";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>();
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadFilePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/file/preview";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferPreviewRequest, DownloadTransferFilePreviewResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferPreviewRequest, DownloadTransferFilePreviewResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>();
      }

      public async Task<Either<DownloadTransferSignatureError, DownloadTransferSignatureResponse>> DownloadFileSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/file/signature";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferSignatureRequest, DownloadTransferSignatureResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferSignatureRequest, DownloadTransferSignatureResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferSignatureError, DownloadTransferSignatureResponse>();
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadFileCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication)
      {
         string url = $"{_baseTransferUrl}/file/ciphertext";
         var (_, response) = withAuthentication
            ? await UseAuthenticationMiddleware(async (token) => await PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest, token))
            : await PostAsync<DownloadTransferCiphertextRequest, DownloadTransferCiphertextResponse>(url, downloadRequest, Maybe<string>.None);

         return response.ExtractErrorCode<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>();
      }
   }
}
