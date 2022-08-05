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

using Crypter.Common.Monads;
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Consent;
using Crypter.Contracts.Features.Contacts;
using Crypter.Contracts.Features.Keys;
using Crypter.Contracts.Features.Metrics;
using Crypter.Contracts.Features.Settings;
using Crypter.Contracts.Features.Transfer;
using Crypter.Contracts.Features.Users;
using System;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Interfaces
{
   public interface ICrypterApiService
   {
      event EventHandler RefreshTokenRejectedEventHandler;

      #region Authentication
      Task<Either<RegistrationError, RegistrationResponse>> RegisterUserAsync(RegistrationRequest registerRequest);
      Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest);
      Task<Either<TestPasswordError, TestPasswordResponse>> TestPasswordAsync(TestPasswordRequest testPasswordRequest);
      Task<Either<RefreshError, RefreshResponse>> RefreshAsync();
      Task<Either<LogoutError, LogoutResponse>> LogoutAsync();
      #endregion

      #region Consent

      Task<Either<DummyError, ConsentToRecoveryKeyRisksResponse>> ConsentToRecoveryKeyRisksAsync();

      #endregion

      #region Contacts
      Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync();
      Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request);
      Task<Either<DummyError, RemoveUserContactResponse>> RemoveUserContactAsync(RemoveUserContactRequest request);
      #endregion

      #region File Transfer
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadAnonymousFilePreviewAsync(Guid id);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadAnonymousFileCiphertextAsync(Guid id, DownloadTransferCiphertextRequest downloadRequest);
      Task<Either<DummyError, UserSentFilesResponse>> GetSentFilesAsync();
      Task<Either<DummyError, UserReceivedFilesResponse>> GetReceivedFilesAsync();
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadUserFilePreviewAsync(Guid id, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadUserFileCiphertextAsync(Guid id, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
      #endregion

      #region Keys
      Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync();
      Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(InsertMasterKeyRequest request);
      Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync();
      Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertDiffieHellmanKeysAsync(InsertKeyPairRequest request);
      #endregion

      #region Message Transfer
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadAnonymousMessagePreviewAsync(Guid id);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadAnonymousMessageCiphertextAsync(Guid id, DownloadTransferCiphertextRequest downloadRequest);
      Task<Either<DummyError, UserSentMessagesResponse>> GetSentMessagesAsync();
      Task<Either<DummyError, UserReceivedMessagesResponse>> GetReceivedMessagesAsync();
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadUserMessagePreviewAsync(Guid id, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadUserMessageCiphertextAsync(Guid id, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
      #endregion

      #region Metrics
      Task<Either<DummyError, DiskMetricsResponse>>GetDiskMetricsAsync();
      #endregion

      #region Search
      Task<Either<DummyError, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParameters searchInfo);
      #endregion

      #region Settings
      Task<Either<DummyError, UserSettingsResponse>> GetUserSettingsAsync();
      Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateContactInfoAsync(UpdateContactInfoRequest request);
      Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateProfileInfoAsync(UpdateProfileRequest request);
      Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateNotificationPreferencesAsync(UpdateNotificationSettingsRequest request);
      Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacySettingsAsync(UpdatePrivacySettingsRequest request);
      Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo);
      #endregion

      #region User
      Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserProfileAsync(string username, bool withAuthentication);
      Task<Either<UploadTransferError, UploadTransferResponse>> SendUserFileTransferAsync(string username, UploadFileTransferRequest request, bool withAuthentication);
      Task<Either<UploadTransferError, UploadTransferResponse>> SendUserMessageTransferAsync(string username, UploadMessageTransferRequest request, bool withAuthentication);
      #endregion
   }
}
