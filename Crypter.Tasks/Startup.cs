using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Crypter.Tasks.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Crypter.DataAccess.EntityFramework;

namespace Crypter.Tasks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // register CronJobs below
            services.AddCronJob<ExpiredItemCleanupJob>(c =>
            {
                c.TimeZoneInfo = TimeZoneInfo.Utc;
                //every minute (frequency used for testing)
                //c.CronExpression = "@every_minute";
                //job runs every 5 minutes
                c.CronExpression = @"*/5 * * * *";
            });

            services.AddSingleton<DataContext>();
            services.AddSingleton<IBaseItemService<MessageItem>, MessageItemService>();
            services.AddSingleton<IBaseItemService<FileItem>, FileItemService>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
