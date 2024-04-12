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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Primitives;
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Features.UserSettings.Events;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.UserSettings.Commands;

public sealed record UpdateContactInformationCommand(Guid UserId, UpdateContactInfoSettingsRequest Request)
    : IEitherRequest<UpdateContactInfoSettingsError, ContactInfoSettings>;

internal sealed class UpdateContactInformationCommandHandler
    : IEitherRequestHandler<UpdateContactInformationCommand, UpdateContactInfoSettingsError, ContactInfoSettings>
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPublisher _publisher;
    private readonly ServerPasswordSettings _serverPasswordSettings;

    public UpdateContactInformationCommandHandler(
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
    
    public async Task<Either<UpdateContactInfoSettingsError, ContactInfoSettings>> Handle(
        UpdateContactInformationCommand request,
        CancellationToken cancellationToken)
    {
        if (!AuthenticationPassword.TryFrom(request.Request.CurrentPassword, out AuthenticationPassword validAuthenticationPassword))
        {
            return UpdateContactInfoSettingsError.InvalidPassword;
        }
        
        bool noEmailAddressProvided = string.IsNullOrEmpty(request.Request.EmailAddress);
        bool validEmailAddressProvided = EmailAddress.TryFrom(request.Request.EmailAddress, out EmailAddress? validEmailAddress);
        bool invalidEmailAddressOption = !(noEmailAddressProvided || validEmailAddressProvided);
        if (invalidEmailAddressOption)
        {
            return UpdateContactInfoSettingsError.InvalidEmailAddress;
        }

        Maybe<EmailAddress> newEmailAddress = validEmailAddressProvided
            ? validEmailAddress
            : Maybe<EmailAddress>.None;

        UserEntity? user = await _dataContext.Users
            .Where(x => x.Id == request.UserId)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (user is null)
        {
            return UpdateContactInfoSettingsError.UserNotFound;
        }

        if (user.ClientPasswordVersion != _serverPasswordSettings.ClientVersion
            || user.ServerPasswordVersion != _passwordHashService.LatestServerPasswordVersion)
        {
            return UpdateContactInfoSettingsError.PasswordNeedsMigration;
        }

        bool correctPasswordProvided = _passwordHashService.VerifySecurePasswordHash(validAuthenticationPassword,
            user.PasswordHash, user.PasswordSalt, _passwordHashService.LatestServerPasswordVersion);
        if (!correctPasswordProvided)
        {
            return UpdateContactInfoSettingsError.InvalidPassword;
        }

        bool differentEmailAddress = (user.EmailAddress ?? string.Empty) !=
                                     newEmailAddress.Match(() => string.Empty, x => x.Value);
        if (differentEmailAddress)
        {
            bool isEmailAddressAvailableForUser = await newEmailAddress.MatchAsync(
                () => true,
                async _ => await _dataContext.Users.IsEmailAddressAvailableAsync(validEmailAddress, CancellationToken.None));

            if (!isEmailAddressAvailableForUser)
            {
                return UpdateContactInfoSettingsError.EmailAddressUnavailable;
            }

            user.EmailAddress = newEmailAddress.Match<string?>(
                () => null,
                x => x.Value);
            user.EmailVerified = false;
            await _dataContext.SaveChangesAsync(CancellationToken.None);

            EmailAddressChangedEvent emailAddressChangedEvent =
                new EmailAddressChangedEvent(request.UserId, newEmailAddress);
            await _publisher.Publish(emailAddressChangedEvent, CancellationToken.None);
        }

        return await Common.GetContactInfoSettingsAsync(_dataContext, request.UserId, cancellationToken)
            .ToEitherAsync(UpdateContactInfoSettingsError.UnknownError);
    }
}
