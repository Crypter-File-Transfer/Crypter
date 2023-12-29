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
using Crypter.Core.Models;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests;

[TestFixture]
public class HangfireBackgroundService_Tests
{
    private WebApplicationFactory<Program> _factory;
    private IServiceScope _scope;
    private DataContext _dataContext;
    private ISender _sender;
    private ILogger<HangfireBackgroundService> _logger;
    
    private Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private Mock<IEmailService> _emailServiceMock;
    private Mock<ITransferRepository> _transferStorageMock;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _emailServiceMock = new Mock<IEmailService>();
        _transferStorageMock = new Mock<ITransferRepository>();

        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        await AssemblySetup.InitializeRespawnerAsync();

        _scope = _factory.Services.CreateScope();
        _dataContext = _scope.ServiceProvider.GetRequiredService<DataContext>();
        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        
        var factory = _scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = factory.CreateLogger<HangfireBackgroundService>();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        _scope.Dispose();
        await _factory.DisposeAsync();
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Verification_Email_Not_Sent_Without_Verification_Parameters()
    {
        _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
                It.IsAny<UserEmailAddressVerificationParameters>()))
            .ReturnsAsync((UserEmailAddressVerificationParameters parameters) => true);

        HangfireBackgroundService sut = new HangfireBackgroundService(_dataContext, _sender, _backgroundJobClientMock.Object,
            _transferStorageMock.Object, _logger);
        await sut.SendEmailVerificationAsync(Guid.NewGuid());

        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<UserEmailAddressVerificationParameters>()),
            Times.Never);
    }

    [Test]
    public async Task Recovery_Email_Not_Sent_Without_Recovery_Parameters()
    {
        _emailServiceMock
            .Setup(x => x.SendAccountRecoveryLinkAsync(
                It.IsAny<UserRecoveryParameters>(),
                It.IsAny<int>()))
            .ReturnsAsync((UserRecoveryParameters parameters, int expirationMinutes) => true);

        HangfireBackgroundService sut = new HangfireBackgroundService(_dataContext, _sender, _backgroundJobClientMock.Object,
            _transferStorageMock.Object, _logger);
        await sut.SendRecoveryEmailAsync("foo@test.com");

        _emailServiceMock.Verify(
            x => x.SendAccountRecoveryLinkAsync(It.IsAny<UserRecoveryParameters>(), It.IsAny<int>()), Times.Never);
    }
}
