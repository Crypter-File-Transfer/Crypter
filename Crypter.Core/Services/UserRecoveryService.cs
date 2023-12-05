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
using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Primitives;
using Crypter.Core.Features.UserRecovery;
using Crypter.Core.Features.UserRecovery.Models;
using Crypter.Core.Models;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;

namespace Crypter.Core.Services;

public interface IUserRecoveryService
{
    Task<Maybe<UserRecoveryParameters>> GenerateRecoveryParametersAsync(string emailAddress);
    byte[] GenerateRecoverySignature(ReadOnlySpan<byte> privateKey, Guid recoveryCode, Username username);
    Task DeleteSavedRecoveryParametersAsync(Guid userId);
    Task<Either<SubmitRecoveryError, Unit>> PerformRecoveryAsync(SubmitRecoveryRequest recoveryRequest);
}

public class UserRecoveryService : IUserRecoveryService
{
    private readonly DataContext _dataContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IUserTransferService _userTransferService;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;

    public UserRecoveryService(DataContext context, IBackgroundJobClient backgroundJobClient,
        ICryptoProvider cryptoProvider, IPasswordHashService passwordHashService,
        IUserTransferService userTransferService, IHangfireBackgroundService hangfireBackgroundService)
    {
        _dataContext = context;
        _backgroundJobClient = backgroundJobClient;
        _cryptoProvider = cryptoProvider;
        _passwordHashService = passwordHashService;
        _userTransferService = userTransferService;
        _hangfireBackgroundService = hangfireBackgroundService;
    }

    public Task<Maybe<UserRecoveryParameters>> GenerateRecoveryParametersAsync(string emailAddress)
    {
        return UserRecoveryQueries.GenerateRecoveryParametersAsync(_dataContext, _cryptoProvider, emailAddress);
    }

    public byte[] GenerateRecoverySignature(ReadOnlySpan<byte> privateKey, Guid recoveryCode, Username username)
    {
        return UserRecoveryQueries.GenerateRecoverySignature(_cryptoProvider, privateKey, recoveryCode, username);
    }

    public Task DeleteSavedRecoveryParametersAsync(Guid userId)
    {
        return UserRecoveryCommands.DeleteRecoveryParametersAsync(_dataContext, userId);
    }

    public Task<Either<SubmitRecoveryError, Unit>> PerformRecoveryAsync(SubmitRecoveryRequest recoveryRequest)
    {
        return UserRecoveryCommands
            .PerformRecoveryAsync(_dataContext, _cryptoProvider, _passwordHashService, recoveryRequest)
            .DoRightAsync(x =>
            {
                if (x.DeleteUserKeys)
                {
                    _backgroundJobClient.Enqueue(() => _hangfireBackgroundService.DeleteUserKeysAsync(x.UserId));
                }

                if (x.DeleteUserReceivedTransfers)
                {
                    _backgroundJobClient.Enqueue(() => _userTransferService.DeleteReceivedTransfersAsync(x.UserId));
                }
            })
            .MapAsync<SubmitRecoveryError, PerformRecoveryResult, Unit>(_ => Unit.Default);
    }
}
