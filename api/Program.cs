using System.Configuration;
using FileIt.App.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileIt.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(
                $"AZURE_FUNCTIONS_ENVIRONMENT: {Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")}"
            );
            // Build a config object, using env vars and JSON providers.
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Production"}.json",
                    optional: true
                )
                .AddEnvironmentVariables()
                .Build();

            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication() // This line is crucial for ASP.NET Core Integration
                //        .ConfigureFunctionsWorkerDefaults() // Configures default settings for the isolated worker
                .ConfigureServices(services =>
                {
                    AppConfig? appConfig = config.GetRequiredSection("App").Get<AppConfig>();
                    if (appConfig == null)
                    {
                        throw new ConfigurationErrorsException(
                            "Configuration is missing or invalid."
                        );
                    }

                    services.AddScoped<App.Providers.IBusProvider, App.Providers.BusProvider>();
                    services.AddScoped<App.Providers.IBlobProvider, App.Providers.BlobProvider>();
                    services.AddScoped<App.Services.ISimpleService, App.Services.SimpleService>();
                    services.AddSingleton(appConfig);

                    services.AddAzureClients(builder =>
                    {
                        builder.AddBlobServiceClient(appConfig.BlobStorageConnectionString);
                        builder.AddServiceBusClient(appConfig.ServiceBusConnectionString);
                    });
                });

            var app = host.Build();
            app.Run();
        }
    }
}
