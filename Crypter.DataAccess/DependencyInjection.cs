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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Crypter.DataAccess;

public static class DependencyInjection
{
    private static readonly string[] RetryableErrorCodes = ["57P01"];

    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ILogger<DataContext> logger = serviceProvider.GetRequiredService<ILogger<DataContext>>();
        
        return services.AddDbContextPool<DataContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptionsBuilder =>
                {
                    npgsqlOptionsBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), RetryableErrorCodes);
                    npgsqlOptionsBuilder.MigrationsHistoryTable(HistoryRepository.DefaultTableName, DataContext.SchemaName);
                })
                .LogTo(
                    filter: (eventId, _) => eventId.Id == CoreEventId.ExecutionStrategyRetrying,
                    logger: eventData =>
                    {
                        ExecutionStrategyEventData? retryEventData = eventData as ExecutionStrategyEventData;
                        IReadOnlyList<Exception>? exceptions = retryEventData?.ExceptionsEncountered;
                        if (retryEventData is not null && exceptions?.Count >= 1)
                        {
                            logger.LogWarning("Retry #{count} with delay {delay} due to error: {error}", exceptions.Count, retryEventData.Delay, exceptions[^1].Message);
                        }
                        else
                        {
                            logger.LogWarning("Retrying due to unknown error");
                        }
                    });
        });
    }
}
