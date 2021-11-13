using Crypter.API.Controllers.Methods;
using Crypter.API.Services;
using Crypter.Core;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.Core.Services.DataAccess;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;

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
         services.AddDbContext<DataContext>();

         services.AddScoped<IUserService, UserService>();
         services.AddScoped<IUserProfileService, UserProfileService>();
         services.AddScoped<IUserPrivacyService, UserPrivacyService>();
         services.AddScoped<IUserPublicKeyPairService<UserX25519KeyPair>, UserX25519KeyPairService>();
         services.AddScoped<IUserPublicKeyPairService<UserEd25519KeyPair>, UserEd25519KeyPairService>();
         services.AddScoped<IUserSearchService, UserSearchService>();
         services.AddScoped<IUserEmailVerificationService, UserEmailVerificationService>();
         services.AddScoped<IBaseTransferService<MessageTransfer>, MessageTransferItemService>();
         services.AddScoped<IBaseTransferService<FileTransfer>, FileTransferItemService>();

         services.AddScoped<IEmailService, EmailService>();

         services.AddHangfire(config =>
              config.UsePostgreSqlStorage(Configuration.GetConnectionString("HangfireConnection")));

         services.AddHangfireServer(options =>
            options.WorkerCount = Configuration.GetValue<int>("HangfireSettings:Workers"));

         var tokenSigningKey = Encoding.UTF8.GetBytes(
            Configuration.GetValue<string>("Secrets:TokenSigningKey"));

         services.AddAuthentication(x =>
         {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
         })
         .AddJwtBearer(x =>
         {
            x.Events = new JwtBearerEvents
            {
               OnTokenValidated = async context =>
               {
                  var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                  var userIdFromJWT = ClaimsParser.ParseUserId(context.Principal);

                  var user = await userService.ReadAsync(userIdFromJWT);
                  if (user == null)
                  {
                     context.Fail("Unauthorized");
                  }
               }
            };
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
               ValidAudience = "crypter.dev",
               ValidIssuer = "crypter.dev/api",
               ValidateIssuerSigningKey = true,
               IssuerSigningKey = new SymmetricSecurityKey(tokenSigningKey),
               ValidateLifetime = true,
               ClockSkew = TimeSpan.Zero
            };
         });

         services.AddCors();
         services.AddControllers();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true)); // allow any origin

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