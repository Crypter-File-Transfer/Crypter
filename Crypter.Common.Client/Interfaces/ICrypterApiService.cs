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
using System.Threading.Tasks;

namespace Crypter.Common.Client.Interfaces
{
   public interface ICrypterApiService
   {
      event EventHandler RefreshTokenRejectedEventHandler;

      #region Authentication
      Task<Either<RegistrationError, RegistrationResponse>> RegisterUserAsync(RegistrationRequest registerRequest);
      Task<Either<LoginError, LoginResponse>>LoginAsync(LoginRequest loginRequest);
      Task<Either<TestPasswordError, TestPasswordResponse>> TestPasswordAsync(TestPasswordRequest testPasswordRequest);
      Task<Either<RefreshError, RefreshResponse>>RefreshAsync();
      Task<Either<LogoutError, LogoutResponse>>LogoutAsync();
      #endregion

      #region Consent
      Task<Either<DummyError, ConsentToRecoveryKeyRisksResponse>> ConsentToRecoveryKeyRisksAsync();
      #endregion

      #region Contacts
      Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync();
      Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request);
      Task<Either<DummyError, RemoveContactResponse>> RemoveUserContactAsync(RemoveContactRequest request);
      #endregion

      #region File Transfer
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadAnonymousFilePreviewAsync(string hashId);
      Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadUserFilePreviewAsync(string hashId, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
      #endregion

      #region Keys
      Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync();
      Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(InsertMasterKeyRequest request);
      Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetPrivateKeyAsync();
      Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertKeyPairAsync(InsertKeyPairRequest request);
      #endregion

      #region Message Transfer
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, EncryptionStream encryptionStream, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadAnonymousMessagePreviewAsync(string hashId);
      Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest);
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadUserMessagePreviewAsync(string hashId, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, StreamDownloadResponse>> DownloadUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
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
      Task<Either<DummyError, UserReceivedFilesResponse>> GetReceivedFilesAsync();
      Task<Either<DummyError, UserSentFilesResponse>> GetSentFilesAsync();
      Task<Either<DummyError, UserReceivedMessagesResponse>> GetReceivedMessagesAsync();
      Task<Either<DummyError, UserSentMessagesResponse>> GetSentMessagesAsync();
      Task<Either<UploadTransferError, UploadTransferResponse>> SendUserFileTransferAsync(string username, UploadFileTransferRequest request, EncryptionStream encryptionStream, bool withAuthentication);
      Task<Either<UploadTransferError, UploadTransferResponse>> SendUserMessageTransferAsync(string username, UploadMessageTransferRequest request, EncryptionStream encryptionStream, bool withAuthentication);
      #endregion
   }
}
