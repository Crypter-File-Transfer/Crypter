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

using System;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Features.UserAuthentication.Events;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record UserRegistrationCommand(RegistrationRequest Request, string DeviceDescription)
    : IEitherRequest<RegistrationError, Unit>;

internal class UserRegistrationCommandHandler
    : IEitherRequestHandler<UserRegistrationCommand, RegistrationError, Unit>
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPublisher _publisher;
    private readonly ServerPasswordSettings _serverPasswordSettings;

    public UserRegistrationCommandHandler(
        DataContext dataContext,
        IPasswordHashService passwordHashService,
        IPublisher publisher,
        IOptions<ServerPasswordSettings> serverPasswordSettings)
    {
        _dataContext = dataContext;
        _passwordHashService = passwordHashService;
        _publisher = publisher;
        _serverPasswordSettings = serverPasswordSettings.Value;
    }
    
    public async Task<Either<RegistrationError, Unit>> Handle(UserRegistrationCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;
        
        return await ValidateRegistrationRequestAsync(request.Request)
            .BindAsync<RegistrationError, ValidRegistrationRequest, UserEntity>(async validRegistrationRequest =>
                {
                    UserEntity newUserEntity = await CreateNewUserEntityAsync(validRegistrationRequest, currentTime);
                    SuccessfulUserRegistrationEvent successfulUserRegistrationEvent = new SuccessfulUserRegistrationEvent(newUserEntity.Id, validRegistrationRequest.EmailAddress, request.DeviceDescription, currentTime);
                    await _publisher.Publish(successfulUserRegistrationEvent, CancellationToken.None);
                    return newUserEntity;
                })
            .DoLeftOrNeitherAsync(
                async error =>
                {
                    FailedUserRegistrationEvent failedUserRegistrationEvent = new FailedUserRegistrationEvent(request.Request.Username, request.Request.EmailAddress, error, request.DeviceDescription, currentTime);
                    await _publisher.Publish(failedUserRegistrationEvent, CancellationToken.None);
                },
                async () =>
                {
                    FailedUserRegistrationEvent failedUserRegistrationEvent = new FailedUserRegistrationEvent(request.Request.Username, request.Request.EmailAddress, RegistrationError.UnknownError, request.DeviceDescription, currentTime);
                    await _publisher.Publish(failedUserRegistrationEvent, CancellationToken.None);
                })
            .BindAsync<RegistrationError, UserEntity, Unit>(_ => Unit.Default);
    }
    
    private readonly struct ValidRegistrationRequest(
        Username username,
        AuthenticationPassword password,
        Maybe<EmailAddress> emailAddress)
    {
        public Username Username { get; } = username;
        public AuthenticationPassword Password { get; } = password;
        public Maybe<EmailAddress> EmailAddress { get; } = emailAddress;
    }
    
    private async Task<Either<RegistrationError, ValidRegistrationRequest>> ValidateRegistrationRequestAsync(RegistrationRequest request)
    {
        if (request.VersionedPassword.Version != _serverPasswordSettings.ClientVersion)
        {
            return RegistrationError.OldPasswordVersion;
        }

        if (!Username.TryFrom(request.Username, out Username? validUsername))
        {
            return RegistrationError.InvalidUsername;
        }

        if (!AuthenticationPassword.TryFrom(request.VersionedPassword.Password, out AuthenticationPassword? validPassword))
        {
            return RegistrationError.InvalidPassword;
        }

        Maybe<EmailAddress> validatedEmailAddress = Maybe<EmailAddress>.None;
        if (EmailAddress.TryFrom(request.EmailAddress!, out EmailAddress? validEmailAddress))
        {
            validatedEmailAddress = validEmailAddress;
        }

        if (!string.IsNullOrEmpty(request.EmailAddress) && validatedEmailAddress.IsNone)
        {
            return RegistrationError.InvalidEmailAddress;
        }

        bool isUsernameAvailable = await _dataContext.Users.IsUsernameAvailableAsync(validUsername);
        if (!isUsernameAvailable)
        {
            return RegistrationError.UsernameTaken;
        }

        bool isEmailAddressAvailable = validatedEmailAddress.IsNone || await _dataContext.IsEmailAddressAvailableAsync(validEmailAddress);
        if (!isEmailAddressAvailable)
        {
            return RegistrationError.EmailAddressTaken;
        }
        
        return new ValidRegistrationRequest(validUsername, validPassword, validatedEmailAddress);
    }

    private async Task<UserEntity> CreateNewUserEntityAsync(ValidRegistrationRequest request, DateTimeOffset currentTime)
    {
        SecurePasswordHashOutput passwordHashOutput = _passwordHashService.MakeSecurePasswordHash(request.Password, _passwordHashService.LatestServerPasswordVersion);
        
        UserEntity newUser = new UserEntity(
            id: Guid.NewGuid(),
            request.Username,
            Maybe<EmailAddress>.None,
            passwordHashOutput.Hash,
            passwordHashOutput.Salt,
            passwordHashOutput.ServerPasswordVersion,
            _serverPasswordSettings.ClientVersion,
            created: currentTime.UtcDateTime,
            lastLogin: DateTime.MinValue);
        
        newUser.Profile = new UserProfileEntity(
            newUser.Id, 
            alias: string.Empty,
            about: string.Empty,
            image: string.Empty);

        newUser.PrivacySetting = new UserPrivacySettingEntity(
            newUser.Id, 
            allowKeyExchangeRequests: true,
            UserVisibilityLevel.Everyone,
            receiveFiles: UserItemTransferPermission.Everyone,
            receiveMessages: UserItemTransferPermission.Everyone);
        
        newUser.NotificationSetting = new UserNotificationSettingEntity(
            newUser.Id, 
            enableTransferNotifications: false,
            emailNotifications: false);
        
        newUser.EmailChange = request.EmailAddress.Match(
            none: (UserEmailChangeEntity?)null,
            some: x => new UserEmailChangeEntity(newUser.Id, x, currentTime.UtcDateTime));

        _dataContext.Users.Add(newUser);
        await _dataContext.SaveChangesAsync();
        return newUser;
    }
}
