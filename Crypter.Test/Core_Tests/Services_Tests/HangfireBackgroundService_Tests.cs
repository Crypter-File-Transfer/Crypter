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
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Services;
using Crypter.Core.Services.Email;
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
    private WebApplicationFactory<Program>? _factory;
    private IServiceScope? _scope;
    private ISender? _sender;
    private ILogger<HangfireBackgroundService>? _logger;
    private Mock<IEmailService>? _emailServiceMock;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _emailServiceMock = new Mock<IEmailService>();

        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        await AssemblySetup.InitializeRespawnerAsync();

        _scope = _factory.Services.CreateScope();
        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        
        ILoggerFactory factory = _scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = factory.CreateLogger<HangfireBackgroundService>();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        _scope?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Verification_Email_Not_Sent_Without_Verification_Parameters()
    {
        _emailServiceMock!
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<EmailAddress>()))
            .ReturnsAsync((string _, string _, EmailAddress _) => true);

        HangfireBackgroundService sut = new HangfireBackgroundService(_sender!, _logger!);
        await sut.SendEmailVerificationAsync(Guid.NewGuid());

        _emailServiceMock.Verify(x =>
                x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()),
                Times.Never);
    }

    [Test]
    public async Task Recovery_Email_Not_Sent_Without_Recovery_Parameters()
    {
        _emailServiceMock!
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<EmailAddress>()))
            .ReturnsAsync((string _, string _, EmailAddress _) => true);

        HangfireBackgroundService sut = new HangfireBackgroundService(_sender!, _logger!);
        await sut.SendRecoveryEmailAsync("foo@test.com");

        _emailServiceMock.Verify(x =>
                x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()),
            Times.Never);
    }
}
