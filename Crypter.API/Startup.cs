/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.API.Models;
using Crypter.API.Services;
using Crypter.API.Startup;
using Crypter.Core;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.Core.Services.DataAccess;
using Crypter.CryptoLib.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CrypterAPI
{
   public class Startup
   {
      public IConfiguration Configuration { get; }

      public Startup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public void ConfigureServices(IServiceCollection services)
      {
         services.AddSingleton<ISimpleEncryptionService, SimpleEncryptionService>();
         services.AddSingleton<ISimpleHashService, SimpleHashService>();
         services.AddSingleton<ISimpleSignatureService, SimpleSignatureService>();
         services.AddSingleton<ITokenService, TokenService>();
         services.AddScoped<IEmailService, EmailService>();
         services.AddScoped<IApiValidationService, ApiValidationService>();

         services.AddDbContext<DataContext>();
         services.AddScoped<IUserService, UserService>();
         services.AddScoped<IUserProfileService, UserProfileService>();
         services.AddScoped<IUserPrivacySettingService, UserPrivacySettingService>();
         services.AddScoped<IUserPublicKeyPairService<UserX25519KeyPair>, UserX25519KeyPairService>();
         services.AddScoped<IUserPublicKeyPairService<UserEd25519KeyPair>, UserEd25519KeyPairService>();
         services.AddScoped<IUserSearchService, UserSearchService>();
         services.AddScoped<IUserEmailVerificationService, UserEmailVerificationService>();
         services.AddScoped<IUserNotificationSettingService, UserNotificationSettingService>();
         services.AddScoped<IUserTokenService, UserTokenService>();
         services.AddScoped<IBaseTransferService<MessageTransfer>, MessageTransferItemService>();
         services.AddScoped<IBaseTransferService<FileTransfer>, FileTransferItemService>();
         services.AddScoped<ISchemaService, SchemaService>();

         var tokenSettings = Configuration.GetSection("TokenSettings").Get<TokenSettings>();
         services.AddSingleton((serviceProvider) => tokenSettings);
         services.AddSingleton((serviceProvider) => Configuration.GetSection("EmailSettings").Get<EmailSettings>());

         services.AddHangfire(config => config.UsePostgreSqlStorage(Configuration.GetConnectionString("HangfireConnection")));
         services.AddHangfireServer(options => options.WorkerCount = Configuration.GetValue<int>("HangfireSettings:Workers"));

         services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearerConfiguration(tokenSettings);

         services.AddCors();
         services.AddControllers();
      }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin());

            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
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
      }
   }
}
