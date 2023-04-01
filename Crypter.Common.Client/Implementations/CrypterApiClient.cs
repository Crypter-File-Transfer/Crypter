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
using System;
using System.Net.Http;

namespace Crypter.Common.Client.Implementations
{
   public class CrypterApiClient : ICrypterApiClient
   {
      private readonly ICrypterHttpClient _crypterHttpClient;
      private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

      private EventHandler _refreshTokenRejectedHandler;

      public IFileTransferRequests FileTransfer { get; init; }
      public IMessageTransferRequests MessageTransfer { get; init; }
      public IMetricsRequests Metrics { get; init; }
      public IUserRequests User { get; init; }
      public IUserAuthenticationRequests UserAuthentication { get; init; }
      public IUserConsentRequests UserConsent { get; init; }
      public IUserContactRequests UserContact { get; init; }
      public IUserKeyRequests UserKey { get; init; }
      public IUserRecoveryRequests UserRecovery { get; init; }
      public IUserSettingRequests UserSetting { get; init; }

      public CrypterApiClient(HttpClient httpClient, ITokenRepository tokenRepository)
      {
         _crypterHttpClient = new CrypterHttpClient(httpClient);
         _crypterAuthenticatedHttpClient = new CrypterAuthenticatedHttpClient(httpClient, tokenRepository, this);

         FileTransfer = new FileTransferRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
         MessageTransfer = new MessageTransferRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
         Metrics = new MetricsRequests(_crypterHttpClient);
         User = new UserRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
         UserAuthentication = new UserAuthenticationRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient, _refreshTokenRejectedHandler);
         UserConsent = new UserConsentRequests(_crypterAuthenticatedHttpClient);
         UserContact = new UserContactRequests(_crypterAuthenticatedHttpClient);
         UserKey = new UserKeyRequests(_crypterAuthenticatedHttpClient);
         UserRecovery = new UserRecoveryRequests(_crypterHttpClient);
         UserSetting = new UserSettingRequests(_crypterHttpClient, _crypterAuthenticatedHttpClient);
      }

      public event EventHandler RefreshTokenRejectedEventHandler
      {
         add => _refreshTokenRejectedHandler = (EventHandler)Delegate.Combine(_refreshTokenRejectedHandler, value);
         remove => _refreshTokenRejectedHandler = (EventHandler)Delegate.Remove(_refreshTokenRejectedHandler, value);
      }
   }
}
