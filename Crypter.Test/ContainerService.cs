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
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Crypter.Test;

internal class ContainerService : IAsyncDisposable
{
    public string CrypterConnectionString { get; private set; }
    public string HangfireConnectionString { get; private set; }

    private PostgreSqlContainer _postgresContainer;

    internal async Task StartPostgresContainerAsync()
    {
        PostgresContainerSettings containerSettings = GetPostgresContainerSettings();

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage(containerSettings.Image)
            .WithPassword(containerSettings.SuperPassword)
            .WithPortBinding(containerSettings.ContainerPort, true)
            .WithBindMount(GetPostgresInitVolume(), "/docker-entrypoint-initdb.d")
            .WithEnvironment("POSTGRES_C_PASSWORD", containerSettings.CrypterUserPassword)
            .WithEnvironment("POSTGRES_HF_PASSWORD", containerSettings.HangfireUserPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready -h 'localhost' -p '5432'"))
            .Build();

        await _postgresContainer.StartAsync();

        NpgsqlConnectionStringBuilder crypterConnectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = _postgresContainer.Hostname,
            Port = _postgresContainer.GetMappedPublicPort(containerSettings.ContainerPort),
            Database = containerSettings.CrypterDatabaseName,
            Username = containerSettings.CrypterUserName,
            Password = containerSettings.CrypterUserPassword
        };

        CrypterConnectionString = crypterConnectionStringBuilder.ConnectionString;

        NpgsqlConnectionStringBuilder hangfireConnectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = _postgresContainer.Hostname,
            Port = _postgresContainer.GetMappedPublicPort(containerSettings.ContainerPort),
            Database = containerSettings.HangfireDatabaseName,
            Username = containerSettings.HangfireUserName,
            Password = containerSettings.HangfireUserPassword
        };

        HangfireConnectionString = hangfireConnectionStringBuilder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.StopAsync();
        }
    }

    internal static PostgresContainerSettings GetPostgresContainerSettings()
    {
        return SettingsReader.GetTestSettings()
            .GetSection("IntegrationTestingOnly:PostgresContainer")
            .Get<PostgresContainerSettings>();
    }

    private static string GetPostgresInitVolume()
    {
        DirectoryInfo repoDirectory = SettingsReader.GetRepoPath();

        string postgresInitVolume = Path.Join(repoDirectory.FullName, "Volumes", "PostgreSQL", "postgres-init-files");
        if (!Path.Exists(postgresInitVolume))
        {
            throw new FileNotFoundException("Failed to find the ./Volumes/PostgreSQL/postgres-init-files directory.");
        }

        return postgresInitVolume;
    }
}

internal class PostgresContainerSettings
{
    public string Image { get; init; }
    public int ContainerPort { get; init; }
    public string SuperPassword { get; init; }
    public string CrypterDatabaseName { get; init; }
    public string CrypterUserName { get; init; }
    public string CrypterUserPassword { get; init; }
    public string HangfireDatabaseName { get; init; }
    public string HangfireUserName { get; init; }
    public string HangfireUserPassword { get; init; }
}
