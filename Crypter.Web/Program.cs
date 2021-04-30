using Crypter.Web.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Crypter.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(_ =>
            {
                return LoadAppSettings("Crypter.Web.appsettings.json")
                            .Get<AppSettings>();
            });

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddBlazorDownloadFile();

            await builder.Build().RunAsync();
        }

        public static IConfigurationRoot LoadAppSettings(string filename)
        {
            var stream = Assembly.GetExecutingAssembly()
                                 .GetManifestResourceStream(filename);

            return new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
        }
    }
}
