﻿/*
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
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Common.Infrastructure;
using Crypter.Core.Models;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crypter.Core.Services;

public interface IUserEmailVerificationService
{
    UserEmailAddressVerificationParameters GenerateVerificationParameters(Guid userId);
    Task<bool> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request);
}

public class UserEmailVerificationService : IUserEmailVerificationService
{
    private readonly ILogger<UserEmailVerificationService> _logger;
    private readonly DataContext _dataContext;
    private readonly ICryptoProvider _cryptoProvider;

    public UserEmailVerificationService(ILogger<UserEmailVerificationService> logger, DataContext dataContext, ICryptoProvider cryptoProvider)
    {
        _logger = logger;
        _dataContext = dataContext;
        _cryptoProvider = cryptoProvider;
    }

    public UserEmailAddressVerificationParameters GenerateVerificationParameters(Guid userId)
    {
        return Features.UserEmailVerification.Common.GenerateEmailAddressVerificationParameters(_cryptoProvider, userId);
    }

    public async Task<bool> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request)
    {
        Guid verificationCode;
        try
        {
            verificationCode = UrlSafeEncoder.DecodeGuidFromUrlSafe(request.Code);
        }
        catch (Exception ex)
        {
            string sanitizedCode = request.Code.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
            _logger.LogError(ex, "An exception occurred while decoding the verification code. VerificationCode: {code}, Message: {message}", sanitizedCode, ex.Message);
            return false;
        }

        UserEntity? userWithVerification = await _dataContext.Users
            .Include(x => x.EmailChange)
            .Where(x => x.EmailChange!.Code == verificationCode)
            .FirstOrDefaultAsync();
        
        if (userWithVerification is null)
        {
            _logger.LogWarning("UserEmailChange record with verification code not found. VerificationCode: {code}", verificationCode);
            return false;
        }

        byte[] signature = UrlSafeEncoder.DecodeBytesFromUrlSafe(request.Signature);
        bool isValidSignature = _cryptoProvider.DigitalSignature.VerifySignature(userWithVerification.EmailChange!.VerificationKey, verificationCode.ToByteArray(), signature);
        if (!isValidSignature)
        {
            _logger.LogWarning("Invalid signature provided for verification code. VerificationCode: {code}", verificationCode);
            return false;
        }

        userWithVerification.EmailAddress = userWithVerification.EmailChange!.EmailAddress;
        _dataContext.UserEmailChangeRequests.Remove(userWithVerification.EmailChange);
        await _dataContext.SaveChangesAsync();

        return true;
    }
}
