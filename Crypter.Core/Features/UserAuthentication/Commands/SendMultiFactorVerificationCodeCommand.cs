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
using Crypter.Common.Primitives;
using Crypter.Core.Services.Email;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record SendMultiFactorVerificationCodeCommand(Guid UserId, Guid MultiFactorChallengeId, int ChallengeExpirationMinutes) : IRequest<bool>;

internal sealed class SendMultiFactorVerificationCodeCommandHandler : IRequestHandler<SendMultiFactorVerificationCodeCommand, bool>
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;

    private const int OneTimePasswordLength = 8;
    private const string OneTimePasswordAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                                   "abcdefghijklmnopqrstuvwxyz" +
                                                   "0123456789";

    public SendMultiFactorVerificationCodeCommandHandler(ICryptoProvider cryptoProvider, DataContext dataContext, IEmailService emailService)
    {
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _emailService = emailService;
    }

    public async Task<bool> Handle(SendMultiFactorVerificationCodeCommand request, CancellationToken cancellationToken)
    {
        string verificationCode = _cryptoProvider.Random.GenerateRandomString(OneTimePasswordLength, OneTimePasswordAlphabet);
        UserMultiFactorChallengeEntity challengeEntity = new UserMultiFactorChallengeEntity(request.MultiFactorChallengeId, request.UserId, verificationCode, DateTime.UtcNow);

        string? verifiedEmailAddress = await _dataContext.Users
            .Where(x => x.Id == request.UserId && x.EmailAddress != null)
            .Select(x => x.EmailAddress)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (!EmailAddress.TryFrom(verifiedEmailAddress!, out EmailAddress validEmailAddress))
        {
            // Return true to indicate we succeeded as much as we could.
            // Returning false may indicate to the caller the command failed for some reason outside our control and should be retried.
            return true;
        }

        bool emailSuccess = await _emailService.SendMultiFactorChallengeEmailAsync(validEmailAddress, verificationCode, request.ChallengeExpirationMinutes);
        if (emailSuccess)
        {
            _dataContext.UserMultiFactorChallenges.Add(challengeEntity);
            await _dataContext.SaveChangesAsync(CancellationToken.None);
        }

        return emailSuccess;
    }
}
