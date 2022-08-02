/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.API.Configuration;
using Crypter.Core;
using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Crypter.CryptoLib.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var tokenSettings = builder.Configuration
   .GetSection("TokenSettings")
   .Get<TokenSettings>();

builder.Services.AddEmailService(options =>
{
   var settings = builder.Configuration
   .GetSection("EmailSettings")
   .Get<EmailSettings>();

   options.Enabled = settings.Enabled;
   options.From = settings.From;
   options.Username = settings.Username;
   options.Password = settings.Password;
   options.Host = settings.Host;
   options.Port = settings.Port;
});

builder.Services.AddTokenService(options =>
{
   options.Audience = tokenSettings.Audience;
   options.Issuer = tokenSettings.Issuer;
   options.SecretKey = tokenSettings.SecretKey;
   options.AuthenticationTokenLifetimeMinutes = tokenSettings.AuthenticationTokenLifetimeMinutes;
   options.SessionTokenLifetimeMinutes = tokenSettings.SessionTokenLifetimeMinutes;
   options.DeviceTokenLifetimeDays = tokenSettings.DeviceTokenLifetimeDays;
});

builder.Services.AddTransferStorageService(options =>
{
   var settings = builder.Configuration
      .GetSection("TransferStorageSettings")
      .Get<TransferStorageSettings>();

   options.AllocatedGB = settings.AllocatedGB;
   options.Location = settings.Location;
});

builder.Services.AddUserAuthenticationService(options =>
{
   var settings = builder.Configuration
      .GetSection("PasswordSettings")
      .Get<ServerPasswordSettings>();

   options.ClientVersion = settings.ClientVersion;
   options.ServerVersions = settings.ServerVersions;
});

builder.Services.AddDbContext<DataContext>();

builder.Services.AddSingleton<ISimpleEncryptionService, SimpleEncryptionService>();
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();

builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
builder.Services.AddScoped<IHangfireBackgroundService, HangfireBackgroundService>();
builder.Services.AddScoped<IServerMetricsService, ServerMetricsService>();
builder.Services.AddScoped<ITransferDownloadService, TransferDownloadService>();
builder.Services.AddScoped<ITransferUploadService, TransferUploadService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IUserContactsService, UserContactsService>();
builder.Services.AddScoped<IUserEmailVerificationService, UserEmailVerificationService>();
builder.Services.AddScoped<IUserKeysService, UserKeysService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserTransferService, UserTransferService>();

builder.Services.AddHangfire(config => config
   .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection"))
   .UseRecommendedSerializerSettings());

builder.Services.AddHangfireServer(options =>
{
   var hangfireSettings = builder.Configuration
      .GetSection("HangfireSettings")
      .Get<HangfireSettings>();

   options.WorkerCount = hangfireSettings.Workers;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearerConfiguration(tokenSettings);

builder.Services.AddCors();
builder.Services.AddControllers()
   .ConfigureApiBehaviorOptions(options =>
   {
      options.SuppressMapClientErrors = true;
   });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerConfiguration.AddSwaggerGenOptions);

builder.WebHost.UseKestrel(options =>
{
   options.Limits.MaxRequestBodySize = null;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
   app.UseDeveloperExceptionPage();
   app.UseCors(x =>
   {
      x.AllowAnyMethod();
      x.AllowAnyHeader();
      x.AllowAnyOrigin();
   });

   app.UseSwagger();
   app.UseSwaggerUI();

   using var serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope();
   var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
   context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
   endpoints.MapControllers();
   endpoints.MapHangfireDashboard();
});

app.Run();
