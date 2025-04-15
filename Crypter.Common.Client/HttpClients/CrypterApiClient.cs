/*
 * Copyright (C) 2025 Crypter File Transfer
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

using System;
using System.Net.Http;
using Crypter.Common.Client.HttpClients.Requests;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Requests;

namespace Crypter.Common.Client.HttpClients;

public class CrypterApiClient : ICrypterApiClient
{
    private EventHandler? _refreshTokenRejectedHandler;

    public IVersionRequests ApiVersion { get; init; }
    public IFileTransferRequests FileTransfer { get; init; }
    public IMessageTransferRequests MessageTransfer { get; init; }
    public IMetricsRequests Metrics { get; init; }
    public ISettingRequests Setting { get; init; }
    public IUserRequests User { get; init; }
    public IUserAuthenticationRequests UserAuthentication { get; init; }
    public IUserConsentRequests UserConsent { get; init; }
    public IUserContactRequests UserContact { get; init; }
    public IUserKeyRequests UserKey { get; init; }
    public IUserRecoveryRequests UserRecovery { get; init; }
    public IUserSettingRequests UserSetting { get; init; }
    public IWellKnownRequests WellKnown { get; init; }

    public CrypterApiClient(HttpClient httpClient, ITokenRepository tokenRepository)
    {
        ICrypterHttpClient crypterHttpClient = new CrypterHttpClient(httpClient);
        ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient = new CrypterAuthenticatedHttpClient(httpClient, tokenRepository, this);
        
        ApiVersion = new VersionRequests(crypterHttpClient);
        FileTransfer = new FileTransferRequests(crypterHttpClient, crypterAuthenticatedHttpClient);
        MessageTransfer = new MessageTransferRequests(crypterHttpClient, crypterAuthenticatedHttpClient);
        Metrics = new MetricsRequests(crypterHttpClient);
        Setting = new SettingRequests(crypterHttpClient, crypterAuthenticatedHttpClient);
        User = new UserRequests(crypterHttpClient, crypterAuthenticatedHttpClient);
        UserAuthentication = new UserAuthenticationRequests(crypterHttpClient, crypterAuthenticatedHttpClient, _refreshTokenRejectedHandler);
        UserConsent = new UserConsentRequests(crypterAuthenticatedHttpClient);
        UserContact = new UserContactRequests(crypterAuthenticatedHttpClient);
        UserKey = new UserKeyRequests(crypterAuthenticatedHttpClient);
        UserRecovery = new UserRecoveryRequests(crypterHttpClient);
        UserSetting = new UserSettingRequests(crypterHttpClient, crypterAuthenticatedHttpClient);
        WellKnown = new WellKnownRequests(crypterHttpClient);
    }

    public event EventHandler RefreshTokenRejectedEventHandler
    {
        add
        {
            _refreshTokenRejectedHandler = (EventHandler?)Delegate.Combine(_refreshTokenRejectedHandler, value);
            UserAuthentication.RefreshTokenRejectedHandler = _refreshTokenRejectedHandler;
        }
        remove
        {
            _refreshTokenRejectedHandler = (EventHandler?)Delegate.Remove(_refreshTokenRejectedHandler, value);
            UserAuthentication.RefreshTokenRejectedHandler = _refreshTokenRejectedHandler;
        }
    }
}
