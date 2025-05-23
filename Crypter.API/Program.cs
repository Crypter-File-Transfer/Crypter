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
using Crypter.API.MetadataProviders;
using Crypter.API.Middleware;
using Crypter.Common.Contracts;
using Crypter.Common.Exceptions;
using Crypter.Core;
using Crypter.Core.Identity;
using Crypter.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

TokenSettings tokenSettings = builder.Configuration
    .GetSection("TokenSettings")
    .Get<TokenSettings>() ?? throw new ConfigurationException("TokenSettings not found");

string hangfireConnectionString = builder.Configuration
    .GetConnectionString("HangfireConnection") ?? throw new ConfigurationException("HangfireConnection not found");

builder.Services.AddCrypterCore(
        builder.Configuration
            .GetSection("AnalyticsSettings")
            .Get<AnalyticsSettings>()
        ?? throw new ConfigurationException("AnalyticsSettings missing from configuration"),
        builder.Configuration
            .GetSection("EmailSettings")
            .Get<EmailSettings>()
        ?? throw new ConfigurationException("EmailSettings missing from configuration"),
        builder.Configuration
            .GetSection("HashIdSettings")
            .Get<HashIdSettings>()
        ?? throw new ConfigurationException("HashIdSettings missing from configuration"),
        builder.Configuration
            .GetSection("PasswordSettings")
            .Get<ServerPasswordSettings>()
        ?? throw new ConfigurationException("PasswordSettings missing from configuration"),
        tokenSettings,
        builder.Configuration
            .GetSection("TransferStorageSettings")
            .Get<TransferStorageSettings>()
        ?? throw new ConfigurationException("TransferStorageSettings missing from configuration"),
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new ConfigurationException("DefaultConnection missing from configuration"),
        hangfireConnectionString)
    .AddBackgroundServer(builder.Configuration.GetSection("HangfireSettings").Get<HangfireSettings>()
                         ?? throw new ConfigurationException("HangfireSettings missing from configuration"));

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerConfiguration>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

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
                .Select(x =>
                    new ErrorResponseItem((int)InfrastructureErrorCode.InvalidModelStateErrorCode, x.ErrorMessage))
                .ToList();

            ErrorResponse errorResponse = new ErrorResponse((int)HttpStatusCode.BadRequest, errors);
            return new BadRequestObjectResult(errorResponse);
        };
    })
    .AddMvcOptions(options => options.ModelMetadataDetailsProviders.Add(new EmptyStringMetaDataProvider()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerConfiguration.AddSwaggerGenOptions);

builder.WebHost.UseKestrel(options => { options.Limits.MaxRequestBodySize = null; });

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

    app.UseSwagger(c =>
    {
        c.RouteTemplate = "api/swagger/{documentname}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Crypter.API");
        c.RoutePrefix = "api/swagger";
    });
}
else
{
    CorsSettings corsSettings = app.Configuration
        .GetSection("CorsSettings")
        .Get<CorsSettings>() ?? throw new ConfigurationException("CorsSettings not found");
    app.UseCors(x =>
    {
        x.AllowAnyMethod();
        x.AllowAnyHeader();
        x.WithOrigins(corsSettings.AllowedOrigins.ToArray());
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllers();

await app.MigrateDatabaseAsync();
app.ScheduleRecurringReports();

await app.RunAsync();
