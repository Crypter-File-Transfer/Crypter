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
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.Metrics;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Crypter.Test.Integration_Tests.Metrics_Tests;

[TestFixture]
internal class PublicStorage_Tests
{
    private WebApplicationFactory<Program>? _factory;
    private ICrypterApiClient? _client;

    [SetUp]
    public async Task SetupTestAsync()
    {
        _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
        (_client, _) = AssemblySetup.SetupCrypterApiClient(_factory.CreateClient());
        await AssemblySetup.InitializeRespawnerAsync();
    }

    [TearDown]
    public async Task TeardownTestAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await AssemblySetup.ResetServerDataAsync();
    }

    [Test]
    public async Task Get_Public_Storage_Metrics_Works()
    {
        Maybe<PublicStorageMetricsResponse> result = await _client!.Metrics.GetPublicStorageMetricsAsync();
        Assert.That(result.IsSome, Is.True);
    }
}
