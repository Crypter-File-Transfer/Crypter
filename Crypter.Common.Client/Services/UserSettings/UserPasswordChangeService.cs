/*
 * Copyright (C) 2024 Crypter File Transfer
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserAuthentication.PasswordChange;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;
using EasyMonads;

namespace Crypter.Common.Client.Services.UserSettings;

public class UserPasswordChangeService : IUserPasswordChangeService
{
    private readonly ICrypterApiClient _crypterApiClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IUserKeysService _userKeysService;
    private readonly IUserPasswordService _userPasswordService;
    private readonly IUserSessionService _userSessionService;

    public UserPasswordChangeService(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserKeysService userKeysService, IUserPasswordService userPasswordService, IUserSessionService userSessionService)
    {
        _crypterApiClient = crypterApiClient;
        _cryptoProvider = cryptoProvider;
        _userKeysService = userKeysService;
        _userPasswordService = userPasswordService;
        _userSessionService = userSessionService;
    }
    
    public async Task<Either<PasswordChangeError, Unit>> ChangePasswordAsync(Password oldPassword, Password newPassword)
    {
        var changePasswordTask = from Session in _userSessionService.Session.ToEither(PasswordChangeError.UnknownError).AsTask()
            let username = Username.From(Session.Username)
            from masterKey in _userKeysService.MasterKey.ToEither(PasswordChangeError.UnknownError).AsTask()
            from newAuthenticationPassword in _userPasswordService.DeriveUserAuthenticationPasswordAsync(username, newPassword, _userPasswordService.CurrentPasswordVersion)
                .ToEitherAsync(PasswordChangeError.PasswordHashFailure)
            from credentialKey in _userPasswordService.DeriveUserCredentialKeyAsync(username, newPassword, _userPasswordService.CurrentPasswordVersion)
                .ToEitherAsync(PasswordChangeError.PasswordHashFailure)
            from oldAuthenticationPassword in _userPasswordService.DeriveUserAuthenticationPasswordAsync(username, oldPassword, _userPasswordService.CurrentPasswordVersion)
                .ToEitherAsync(PasswordChangeError.PasswordHashFailure)
            let nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize)
            let encryptedMasterKey = _cryptoProvider.Encryption.Encrypt(credentialKey, nonce, masterKey)
            from passwordChangeResult in ChangePasswordRecursiveAsync(username, oldPassword, [oldAuthenticationPassword], newAuthenticationPassword, encryptedMasterKey, nonce)
            select passwordChangeResult;

        return await changePasswordTask;
    }

    private async Task<Either<PasswordChangeError, Unit>> ChangePasswordRecursiveAsync(Username username, Password oldPassword, List<VersionedPassword> oldPasswords, VersionedPassword newPassword, byte[] encryptedMasterKey, byte[] nonce)
    {
        return await SendPasswordChangeRequest(oldPasswords, newPassword, encryptedMasterKey, nonce)
            .MatchAsync(
                async error =>
                {
                    int oldestPasswordVersionAttempted = oldPasswords.Min(x => x.Version);
                    if (error == PasswordChangeError.InvalidOldPasswordVersion && oldestPasswordVersionAttempted > 0)
                    {
                        return await _userPasswordService
                            .DeriveUserAuthenticationPasswordAsync(username, oldPassword, oldestPasswordVersionAttempted - 1)
                            .MatchAsync(
                                () => PasswordChangeError.PasswordHashFailure,
                                async previousVersionedPassword =>
                                {
                                    oldPasswords.Add(previousVersionedPassword);
                                    return await ChangePasswordRecursiveAsync(username, oldPassword, oldPasswords, newPassword, encryptedMasterKey, nonce);
                                });
                    }
                    return error;
                },
                response => response,
                PasswordChangeError.UnknownError);
    }

    private async Task<Either<PasswordChangeError, Unit>> SendPasswordChangeRequest(List<VersionedPassword> oldPasswords, VersionedPassword newPassword, byte[] encryptedMasterKey, byte[] nonce)
    {
        PasswordChangeRequest request = new PasswordChangeRequest(oldPasswords, newPassword, encryptedMasterKey, nonce);
        return await _crypterApiClient.UserAuthentication.ChangePasswordAsync(request);
    }
}
