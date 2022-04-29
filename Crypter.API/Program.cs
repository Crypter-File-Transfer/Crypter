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
using Crypter.API.Models;
using Crypter.API.Services;
using Crypter.Core;
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
using Crypter.Core.Services;
using Crypter.Core.Services.DataAccess;
using Crypter.CryptoLib.Services;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISimpleEncryptionService, SimpleEncryptionService>();
builder.Services.AddSingleton<ISimpleHashService, SimpleHashService>();
builder.Services.AddSingleton<ISimpleSignatureService, SimpleSignatureService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IApiValidationService, ApiValidationService>();
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<IUserPrivacyService, UserPrivacyService>();
builder.Services.AddScoped<IHangfireBackgroundService, HangfireBackgroundService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();

builder.Services.AddDbContext<DataContext>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserPrivacySettingService, UserPrivacySettingService>();
builder.Services.AddScoped<IUserPublicKeyPairService<UserX25519KeyPair>, UserX25519KeyPairService>();
builder.Services.AddScoped<IUserPublicKeyPairService<UserEd25519KeyPair>, UserEd25519KeyPairService>();
builder.Services.AddScoped<IUserEmailVerificationService, UserEmailVerificationService>();
builder.Services.AddScoped<IUserNotificationSettingService, UserNotificationSettingService>();
builder.Services.AddScoped<IBaseTransferService<IMessageTransfer>, MessageTransferItemService>();
builder.Services.AddScoped<IBaseTransferService<IFileTransfer>, FileTransferItemService>();

builder.Services.AddMediatR(Assembly.GetAssembly(typeof(Crypter.Core.DataContext))!);

var configuration = builder.Configuration;
var tokenSettings = configuration.GetSection("TokenSettings").Get<TokenSettings>();
builder.Services.AddSingleton((serviceProvider) => tokenSettings);
builder.Services.AddSingleton((serviceProvider) => configuration.GetSection("EmailSettings").Get<EmailSettings>());

builder.Services.AddHangfire(config => config
   .UsePostgreSqlStorage(configuration.GetConnectionString("HangfireConnection"))
   .UseRecommendedSerializerSettings());
builder.Services.AddHangfireServer(options => options.WorkerCount = configuration.GetValue<int>("HangfireSettings:Workers"));

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
   app.UseDeveloperExceptionPage();
   app.UseCors(x =>
      x.AllowAnyMethod()
      .AllowAnyHeader()
      .AllowAnyOrigin());

   app.UseSwagger();
   app.UseSwaggerUI();

   using var serviceScope = app.Services.GetService<IServiceScopeFactory>()!.CreateScope();
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
