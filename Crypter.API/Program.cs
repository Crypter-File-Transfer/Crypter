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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Crypter.API.Configuration;
using Crypter.API.Middleware;
using Crypter.Common.Contracts;
using Crypter.Core;
using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

TokenSettings tokenSettings = builder.Configuration
   .GetSection("TokenSettings")
   .Get<TokenSettings>();

string hangfireConnectionString = builder.Configuration
   .GetConnectionString("HangfireConnection");

builder.Services.AddCrypterCore(
   builder.Configuration
      .GetSection("EmailSettings")
      .Get<EmailSettings>(),

   builder.Configuration
      .GetSection("HashIdSettings")
      .Get<HashIdSettings>(),

   builder.Configuration
      .GetSection("PasswordSettings")
      .Get<ServerPasswordSettings>(),

   tokenSettings,

   builder.Configuration
      .GetSection("TransferStorageSettings")
      .Get<TransferStorageSettings>(),

   builder.Configuration.GetConnectionString("DefaultConnection"),

   hangfireConnectionString)
   .AddBackgroundServer(builder.Configuration.GetSection("HangfireSettings").Get<HangfireSettings>());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearerConfiguration(tokenSettings);

builder.Services.AddCors();
builder.Services.AddControllers()
   .ConfigureApiBehaviorOptions(options =>
   {
      options.SuppressMapClientErrors = true;
      options.InvalidModelStateResponseFactory = actionContext =>
      {
         List<ErrorResponseItem> errors = actionContext.ModelState.Values
            .Where(x => x.Errors.Count > 0)
            .SelectMany(x => x.Errors)
            .Select(x => new ErrorResponseItem((int)InfrastructureErrorCode.InvalidModelStateErrorCode, x.ErrorMessage))
            .ToList();

         ErrorResponse errorResponse = new ErrorResponse((int)HttpStatusCode.BadRequest, errors);
         return new BadRequestObjectResult(errorResponse);
      };
   });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerConfiguration.AddSwaggerGenOptions);

builder.WebHost.UseKestrel(options =>
{
   options.Limits.MaxRequestBodySize = null;
});

WebApplication app = builder.Build();

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

   using IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope();
   using DataContext context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
   context.Database.EnsureCreated();
}
else
{
   app.UseCors(x =>
   {
      x.AllowAnyMethod();
      x.AllowAnyHeader();
      x.WithOrigins("https://*.crypter.dev")
         .SetIsOriginAllowedToAllowWildcardSubdomains();
   });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllers();

app.Run();
