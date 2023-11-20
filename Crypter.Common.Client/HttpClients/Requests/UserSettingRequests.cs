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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using Crypter.Common.Contracts.Features.UserSettings.PrivacySettings;
using Crypter.Common.Contracts.Features.UserSettings.ProfileSettings;
using EasyMonads;

namespace Crypter.Common.Client.HttpClients.Requests;

public class UserSettingRequests : IUserSettingRequests
{
    private readonly ICrypterHttpClient _crypterHttpClient;
    private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

    public UserSettingRequests(ICrypterHttpClient crypterHttpClient,
        ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient)
    {
        _crypterHttpClient = crypterHttpClient;
        _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
    }

    public Task<Maybe<ProfileSettings>> GetProfileSettingsAsync()
    {
        const string url = "api/user/setting/profile";
        return _crypterAuthenticatedHttpClient.GetMaybeAsync<ProfileSettings>(url);
    }

    public Task<Either<SetProfileSettingsError, ProfileSettings>> SetProfileSettingsAsync(
        ProfileSettings newProfileSettings)
    {
        const string url = "api/user/setting/profile";
        return _crypterAuthenticatedHttpClient.PutEitherAsync<ProfileSettings, ProfileSettings>(url, newProfileSettings)
            .ExtractErrorCode<SetProfileSettingsError, ProfileSettings>();
    }

    public Task<Maybe<ContactInfoSettings>> GetContactInfoSettingsAsync()
    {
        const string url = "api/user/setting/contact";
        return _crypterAuthenticatedHttpClient.GetMaybeAsync<ContactInfoSettings>(url);
    }

    public Task<Either<UpdateContactInfoSettingsError, ContactInfoSettings>> UpdateContactInfoSettingsAsync(
        UpdateContactInfoSettingsRequest newContactInfoSettings)
    {
        const string url = "api/user/setting/contact";
        return _crypterAuthenticatedHttpClient
            .PostEitherAsync<UpdateContactInfoSettingsRequest, ContactInfoSettings>(url, newContactInfoSettings)
            .ExtractErrorCode<UpdateContactInfoSettingsError, ContactInfoSettings>();
    }

    public Task<Maybe<NotificationSettings>> GetNotificationSettingsAsync()
    {
        const string url = "api/user/setting/notification";
        return _crypterAuthenticatedHttpClient.GetMaybeAsync<NotificationSettings>(url);
    }

    public Task<Either<UpdateNotificationSettingsError, NotificationSettings>> UpdateNotificationSettingsAsync(
        NotificationSettings newNotificationSettings)
    {
        const string url = "api/user/setting/notification";
        return _crypterAuthenticatedHttpClient
            .PostEitherAsync<NotificationSettings, NotificationSettings>(url, newNotificationSettings)
            .ExtractErrorCode<UpdateNotificationSettingsError, NotificationSettings>();
    }

    public Task<Maybe<PrivacySettings>> GetPrivacySettingsAsync()
    {
        const string url = "api/user/setting/privacy";
        return _crypterAuthenticatedHttpClient.GetMaybeAsync<PrivacySettings>(url);
    }

    public Task<Either<SetPrivacySettingsError, PrivacySettings>> SetPrivacySettingsAsync(
        PrivacySettings newPrivacySettings)
    {
        const string url = "api/user/setting/privacy";
        return _crypterAuthenticatedHttpClient.PutEitherAsync<PrivacySettings, PrivacySettings>(url, newPrivacySettings)
            .ExtractErrorCode<SetPrivacySettingsError, PrivacySettings>();
    }

    public Task<Either<VerifyEmailAddressError, Unit>> VerifyUserEmailAddressAsync(
        VerifyEmailAddressRequest verificationInfo)
    {
        const string url = "api/user/setting/contact/verify";
        return _crypterHttpClient.PostEitherUnitResponseAsync(url, verificationInfo)
            .ExtractErrorCode<VerifyEmailAddressError, Unit>();
    }
}
