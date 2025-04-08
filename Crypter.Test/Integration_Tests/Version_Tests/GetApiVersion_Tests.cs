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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.Version;
using EasyMonads;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.Version_Tests;

[TestFixture]
public sealed class GetApiVersion_Tests
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
    public async Task Get_Api_Version_Works()
    {
        Maybe<VersionResponse> result = await _client!.ApiVersion.GetApiVersionAsync();
        Assert.That(result.IsSome, Is.True);
    }
}
