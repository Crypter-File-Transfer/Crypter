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

using System.Threading.Tasks;
using Crypter.Core.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Features_Tests;

[TestFixture]
internal class ApplicationAnalyticsReportQuery_Tests
{
    private WebApplicationFactory<Program>? _factory;
    private IServiceScope? _scope;
    private IMediator? _mediator;
    
    [SetUp]
    public async Task SetupTestAsync()
    {
        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        await AssemblySetup.InitializeRespawnerAsync();

        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
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
    public void Application_Analytics_Report_Query_Works()
    {
        ApplicationAnalyticsReportQuery query = new ApplicationAnalyticsReportQuery(1);
        Assert.DoesNotThrowAsync(async () => await _mediator!.Send(query));
    }
}
