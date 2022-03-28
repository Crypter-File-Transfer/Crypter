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
using System.Threading.Tasks;

namespace Crypter.ClientServices.Interfaces
{
   public interface ICrypterApiService
   {
      // Authentication
      Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest loginRequest);
      Task<Either<RefreshError, RefreshResponse>> RefreshAsync();
      Task<Either<LogoutError, LogoutResponse>> LogoutAsync(LogoutRequest logoutRequest);

      // Metrics
      Task<Either<DummyError, DiskMetricsResponse>> GetDiskMetricsAsync();

      // User
      Task<Either<UserRegisterError, UserRegisterResponse>> RegisterUserAsync(UserRegisterRequest registerRequest);
      Task<Either<GetUserProfileError, GetUserProfileResponse>> GetUserPublicProfileAsync(string username, bool withAuthentication);
      Task<Either<DummyError, UserSettingsResponse>> GetUserSettingsAsync();
      Task<Either<UpdateProfileError, UpdateProfileResponse>> UpdateUserProfileInfoAsync(UpdateProfileRequest request);
      Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(UpdateContactInfoRequest request);
      Task<Either<UpdatePrivacySettingsError, UpdatePrivacySettingsResponse>> UpdateUserPrivacyAsync(UpdatePrivacySettingsRequest request);
      Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpdateUserNotificationAsync(UpdateNotificationSettingsRequest request);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetUserX25519PrivateKeyAsync();
      Task<Either<UpdateKeysError, UpdateKeysResponse>> InsertUserX25519KeysAsync(UpdateKeysRequest request);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetUserEd25519PrivateKeyAsync();
      Task<Either<UpdateKeysError, UpdateKeysResponse>> InsertUserEd25519KeysAsync(UpdateKeysRequest request);
      Task<Either<DummyError, UserSentMessagesResponse>> GetUserSentMessagesAsync();
      Task<Either<DummyError, UserSentFilesResponse>> GetUserSentFilesAsync();
      Task<Either<DummyError, UserReceivedMessagesResponse>> GetUserReceivedMessagesAsync();
      Task<Either<DummyError, UserReceivedFilesResponse>> GetUserReceivedFilesAsync();
      Task<Either<DummyError, UserSearchResponse>> GetUserSearchResultsAsync(UserSearchParameters searchInfo);
      Task<Either<VerifyEmailAddressError, VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo);
      Task<Either<DummyError, GetUserContactsResponse>> GetUserContactsAsync();
      Task<Either<AddUserContactError, AddUserContactResponse>> AddUserContactAsync(AddUserContactRequest request);
      Task<Either<DummyError, RemoveUserContactResponse>> RemoveUserContactAsync(RemoveUserContactRequest request);

      // Transfer
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(UploadMessageTransferRequest uploadRequest, Maybe<string> recipient, bool withAuthentication);
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(UploadFileTransferRequest uploadRequest, Maybe<string> recipient, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadMessagePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication);
      Task<Either<DownloadTransferSignatureError, DownloadTransferSignatureResponse>> DownloadMessageSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadMessageCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadFilePreviewAsync(DownloadTransferPreviewRequest downloadRequest, bool withAuthentication);
      Task<Either<DownloadTransferSignatureError, DownloadTransferSignatureResponse>> DownloadFileSignatureAsync(DownloadTransferSignatureRequest downloadRequest, bool withAuthentication);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> DownloadFileCiphertextAsync(DownloadTransferCiphertextRequest downloadRequest, bool withAuthentication);
   }
}
