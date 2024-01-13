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

using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests;

[TestFixture]
public class EmailService_Tests
{
    private IOptions<EmailSettings> _defaultEmailSettings;
    private ILogger<EmailService> _logger;

    [SetUp]
    public void Setup()
    {
        EmailSettings settings = new EmailSettings();
        _defaultEmailSettings = Options.Create(settings);
        
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var factory = serviceProvider.GetService<ILoggerFactory>();
        _logger = factory.CreateLogger<EmailService>();
    }

    [Test]
    public async Task ServiceDisabled_SendAsync_ReturnsTrue()
    {
        var sut = new EmailService(_defaultEmailSettings, _logger);
        var emailAddress = EmailAddress.From("jack@crypter.dev");
        var result = await sut.SendAsync("foo", "bar", emailAddress);
        Assert.That(result, Is.True);
    }
}
