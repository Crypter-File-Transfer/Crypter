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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Core.Services.Email;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserEmailVerification.Commands;

/// <summary>
/// A command that will attempt to send a verification email to the provided
/// user.
///
/// Returns 'true' if the email was successfully sent or if the user's current
/// state is such that they should not receive a verification email.
/// 
/// Returns 'false' if the email should be sent, but failed to send.
/// </summary>
/// <param name="UserId"></param>
public sealed record SendVerificationEmailCommand(Guid UserId) : IRequest<bool>;

internal sealed class SendVerificationEmailCommandHandler
    : IRequestHandler<SendVerificationEmailCommand, bool>
{
    private const int VerificationExpirationMinutes = 30;
    
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;

    public SendVerificationEmailCommandHandler(
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        IHangfireBackgroundService hangfireBackgroundService)
    {
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient;
        _hangfireBackgroundService = hangfireBackgroundService;
    }
    
    public async Task<bool> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        UserEntity? userWithEmailChange = await _dataContext.Users
            .Include(x => x.EmailChange)
            .Where(x => x.Id == request.UserId)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (userWithEmailChange?.EmailChange is null)
        {
            return true;
        }

        if (!EmailAddress.TryFrom(userWithEmailChange.EmailChange.EmailAddress, out EmailAddress? emailAddress))
        {
            return false;
        }
        
        UserEmailAddressVerificationParameters parameters = Common.GenerateEmailAddressVerificationParameters(_cryptoProvider, request.UserId);
        bool deliverySuccess = await _emailService.SendVerificationEmailAsync(parameters, emailAddress, VerificationExpirationMinutes);
        if (deliverySuccess)
        {
            DateTimeOffset currentTime = DateTimeOffset.Now;
            
            userWithEmailChange.EmailChange.Code = parameters.VerificationCode;
            userWithEmailChange.EmailChange.VerificationKey = parameters.VerificationKey;
            userWithEmailChange.EmailChange.VerificationSent = currentTime.UtcDateTime;
            await _dataContext.SaveChangesAsync(CancellationToken.None);

            DateTimeOffset verificationExpiration = currentTime.AddMinutes(VerificationExpirationMinutes);
            _backgroundJobClient.Schedule(() => _hangfireBackgroundService.DeleteEmailChangeRequestAsync(request.UserId, parameters.VerificationCode), verificationExpiration);
        }

        return deliverySuccess;
    }
}
