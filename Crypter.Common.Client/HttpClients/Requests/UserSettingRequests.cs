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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Monads;
using System.Threading.Tasks;

namespace Crypter.Common.Client.HttpClients.Requests
{
   public class UserSettingRequests : IUserSettingRequests
   {
      private readonly ICrypterHttpClient _crypterHttpClient;
      private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

      public UserSettingRequests(ICrypterHttpClient crypterHttpClient, ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient)
      {
         _crypterHttpClient = crypterHttpClient;
         _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
      }

      public Task<Maybe<UserSettings>> GetUserSettingsAsync()
      {
         string url = "api/user/setting";
         return _crypterAuthenticatedHttpClient.GetMaybeAsync<UserSettings>(url);
      }

      public Task<Either<UpdateContactInfoError, Unit>> UpdateContactInfoAsync(UpdateContactInfoRequest request)
      {
         string url = "api/user/setting/contact";
         return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, request)
            .ExtractErrorCode<UpdateContactInfoError, Unit>();
      }

      public Task<Either<VerifyEmailAddressError, Unit>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest verificationInfo)
      {
         string url = "api/user/setting/contact/verify";
         return _crypterHttpClient.PostEitherUnitResponseAsync(url, verificationInfo)
            .ExtractErrorCode<VerifyEmailAddressError, Unit>();
      }

      public Task<Either<UpdateNotificationSettingsError, Unit>> UpdateNotificationPreferencesAsync(UpdateNotificationSettingsRequest request)
      {
         string url = "api/user/setting/notification";
         return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, request)
            .ExtractErrorCode<UpdateNotificationSettingsError, Unit>();
      }

      public Task<Either<UpdatePrivacySettingsError, Unit>> UpdateUserPrivacySettingsAsync(UpdatePrivacySettingsRequest request)
      {
         string url = "api/user/setting/privacy";
         return _crypterAuthenticatedHttpClient.PostEitherUnitResponseAsync(url, request)
            .ExtractErrorCode<UpdatePrivacySettingsError, Unit>();
      }
   }
}
