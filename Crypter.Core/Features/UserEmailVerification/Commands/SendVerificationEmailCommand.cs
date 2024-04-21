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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Core.Models;
using Crypter.Core.Services.Email;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;

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
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;

    public SendVerificationEmailCommandHandler(
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IEmailService emailService)
    {
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _emailService = emailService;
    }
    
    public async Task<bool> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        return await Common
            .GenerateEmailAddressVerificationParametersAsync(_dataContext, _cryptoProvider, request.UserId)
            .BindAsync<UserEmailAddressVerificationParameters, bool>(async parameters =>
            {
                bool deliverySuccess = await _emailService.SendVerificationEmailAsync(parameters);
                if (deliverySuccess)
                {
                    UserEmailVerificationEntity newEntity = new UserEmailVerificationEntity(parameters.UserId,
                        parameters.VerificationCode, parameters.VerificationKey, DateTime.UtcNow);
                    _dataContext.UserEmailVerifications.Add(newEntity);
                    await _dataContext.SaveChangesAsync(CancellationToken.None);
                }

                return deliverySuccess;
            }).SomeOrDefaultAsync(true);
    }
}
