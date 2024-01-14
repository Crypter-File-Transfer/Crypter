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
using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Core.Services.UserSettings;
using Crypter.Core.Settings;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using Crypter.DataAccess;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crypter.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCrypterCore(this IServiceCollection services,
        EmailSettings emailSettings,
        HashIdSettings hashIdSettings,
        ServerPasswordSettings serverPasswordSettings,
        TokenSettings tokenSettings,
        TransferStorageSettings transferStorageSettings,
        string defaultConnectionString,
        string hangfireConnectionString)
    {
        services.AddDataAccess(defaultConnectionString);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(AssemblyInfo)));

        services.TryAddSingleton<IPasswordHashService, PasswordHashService>();
        services.TryAddSingleton<ICryptoProvider, DefaultCryptoProvider>();

        services.TryAddScoped<IHangfireBackgroundService, HangfireBackgroundService>();
        services.TryAddScoped<ITransferDownloadService, TransferDownloadService>();
        services.TryAddScoped<ITransferUploadService, TransferUploadService>();
        services.TryAddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.TryAddScoped<IUserEmailVerificationService, UserEmailVerificationService>();
        services.TryAddScoped<IUserTransferService, UserTransferService>();
        services.TryAddScoped<IUserProfileSettingsService, UserProfileSettingsService>();
        services.TryAddScoped<IUserContactInfoSettingsService, UserContactInfoSettingsService>();
        services.TryAddScoped<IUserNotificationSettingsService, UserNotificationSettingService>();
        services.TryAddScoped<IUserPrivacySettingsService, UserPrivacySettingsService>();

        services.AddEmailService(options =>
        {
            options.Enabled = emailSettings.Enabled;
            options.From = emailSettings.From;
            options.Username = emailSettings.Username;
            options.Password = emailSettings.Password;
            options.Host = emailSettings.Host;
            options.Port = emailSettings.Port;
        });

        services.AddHashIdService(options => { options.Salt = hashIdSettings.Salt; });

        services.AddUserAuthenticationService(options =>
        {
            options.ClientVersion = serverPasswordSettings.ClientVersion;
            options.ServerVersions = serverPasswordSettings.ServerVersions;
        });

        services.AddTokenService(options =>
        {
            options.Audience = tokenSettings.Audience;
            options.Issuer = tokenSettings.Issuer;
            options.SecretKey = tokenSettings.SecretKey;
            options.AuthenticationTokenLifetimeMinutes = tokenSettings.AuthenticationTokenLifetimeMinutes;
            options.SessionTokenLifetimeMinutes = tokenSettings.SessionTokenLifetimeMinutes;
            options.DeviceTokenLifetimeDays = tokenSettings.DeviceTokenLifetimeDays;
        });

        services.AddTransferRepository(options =>
        {
            options.AllocatedGB = transferStorageSettings.AllocatedGB;
            options.Location = transferStorageSettings.Location;
        });

        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options => { options.UseNpgsqlConnection(hangfireConnectionString); })
            .UseRecommendedSerializerSettings());

        return services;
    }

    public static IServiceCollection AddBackgroundServer(this IServiceCollection services,
        HangfireSettings hangfireSettings) => services.AddHangfireServer(options => { options.WorkerCount = hangfireSettings.Workers; });

    public static async Task<IApplicationBuilder> MigrateDatabaseAsync(this WebApplication webApplication, DatabaseSettings databaseSettings)
    {
        if (databaseSettings.MigrateOnStartup)
        {
            using IServiceScope serviceScope = webApplication.Services.GetService<IServiceScopeFactory>().CreateScope();
            await using DataContext context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
            await context.Database.MigrateAsync();
        }

        return webApplication;
    }
}
