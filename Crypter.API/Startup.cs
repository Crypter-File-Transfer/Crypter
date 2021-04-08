using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CrypterAPI.Models;
using MySqlConnector;

namespace CrypterAPI
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
         services.AddTransient<CrypterDB>(_ => new CrypterDB(Configuration["ConnectionStrings:DefaultConnection"]));

         //// Adds db context to DI container and specifies context uses in-memory database
         //services.AddDbContext<UploadItemContext>(opt => opt.UseInMemoryDatabase("CrypterDB"));
         //services.AddControllers();
         ////text upload db context
         //services.AddDbContext<TextUploadItemContext>(opt => opt.UseInMemoryDatabase("CrypterMessagesDB"));
         //services.AddControllers();
         //// file upload db context
         //services.AddDbContext<FileUploadItemContext>(opt => opt.UseInMemoryDatabase("CrypterFileDB"));

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