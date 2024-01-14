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

using System;
using System.Threading.Tasks;
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Primitives;
using EasyMonads;

namespace Crypter.Common.Client.Services.UserSettings;

public class UserContactInfoSettingsService : IUserContactInfoSettingsService, IDisposable
{
    private readonly ICrypterApiClient _crypterApiClient;
    private readonly IUserPasswordService _userPasswordService;
    private readonly IUserSessionService _userSessionService;

    private Maybe<ContactInfoSettings> _contactInfoSettings;

    private EventHandler<UserContactInfoChangedEventArgs>? _userContactInfoChangedEventHandler;

    public UserContactInfoSettingsService(ICrypterApiClient crypterApiClient, IUserPasswordService userPasswordService,
        IUserSessionService userSessionService)
    {
        _crypterApiClient = crypterApiClient;
        _userPasswordService = userPasswordService;
        _userSessionService = userSessionService;
        
        _userSessionService.UserLoggedOutEventHandler += Recycle;
    }

    public async Task<Maybe<ContactInfoSettings>> GetContactInfoSettingsAsync()
    {
        if (_contactInfoSettings.IsNone)
        {
            _contactInfoSettings = await _crypterApiClient.UserSetting.GetContactInfoSettingsAsync();
        }

        return _contactInfoSettings;
    }

    public async Task<Either<UpdateContactInfoSettingsError, ContactInfoSettings>> UpdateContactInfoSettingsAsync(
        Maybe<EmailAddress> emailAddress, Password currentPassword)
    {
        string rawUsername = _userSessionService.Session.Match(
            () => string.Empty,
            x => x.Username);

        if (!Username.TryFrom(rawUsername, out Username username))
        {
            return UpdateContactInfoSettingsError.InvalidUsername;
        }

        return await _userPasswordService.DeriveUserAuthenticationPasswordAsync(username, currentPassword,
                _userPasswordService.CurrentPasswordVersion)
            .ToEitherAsync(UpdateContactInfoSettingsError.PasswordHashFailure)
            .MapAsync(async x =>
            {
                UpdateContactInfoSettingsRequest request =
                    new UpdateContactInfoSettingsRequest(emailAddress, x.Password);
                Either<UpdateContactInfoSettingsError, ContactInfoSettings> response =
                    await _crypterApiClient.UserSetting.UpdateContactInfoSettingsAsync(request);
                _contactInfoSettings = response.ToMaybe();

                response.DoRight(updatedContactInfoSettings =>
                {
                    Maybe<EmailAddress> newEmailAddress = EmailAddress.TryFrom(updatedContactInfoSettings.EmailAddress,
                        out EmailAddress newValidEmailAddress)
                        ? newValidEmailAddress
                        : Maybe<EmailAddress>.None;
                    HandleUserContactInfoChangedEvent(newEmailAddress, updatedContactInfoSettings.EmailAddressVerified);
                });

                return response;
            });
    }

    public event EventHandler<UserContactInfoChangedEventArgs> UserContactInfoChangedEventHandler
    {
        add => _userContactInfoChangedEventHandler =
            (EventHandler<UserContactInfoChangedEventArgs>)Delegate.Combine(_userContactInfoChangedEventHandler, value);
        remove => _userContactInfoChangedEventHandler =
            (EventHandler<UserContactInfoChangedEventArgs>?)Delegate.Remove(_userContactInfoChangedEventHandler, value);
    }

    private void HandleUserContactInfoChangedEvent(Maybe<EmailAddress> emailAddress, bool emailAddressVerified) =>
        _userContactInfoChangedEventHandler?.Invoke(this,
            new UserContactInfoChangedEventArgs(emailAddress, emailAddressVerified));

    private void Recycle(object? _, EventArgs __)
    {
        _contactInfoSettings = Maybe<ContactInfoSettings>.None;
    }

    public void Dispose()
    {
        _userSessionService.UserLoggedOutEventHandler -= Recycle;
        GC.SuppressFinalize(this);
    }
}
