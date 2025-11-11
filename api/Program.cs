using System.Configuration;
using System.Text.Json;
using Azure.Identity;
using FileIt.App.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileIt.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var env =
                Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Production";
            Console.WriteLine($"AZURE_FUNCTIONS_ENVIRONMENT: {env}");
            bool isProduction = env.ToLower().Equals("production");
            // Build a config object, using env vars and JSON providers.
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string azureFunctionsEnvironment = config.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? string.Empty;
            string azureStorageConnectionString = config.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING") ?? string.Empty;
            string azureServiceBusConnectionString = config.GetValue<string>("ServiceBus") ?? string.Empty;

            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication() // This line is crucial for ASP.NET Core Integration
                // .ConfigureLogging(logging =>
                // {
                //     logging.AddConsole();
                //     logging.AddApplicationInsights();
                // })
                .ConfigureServices(services =>
                {
                    AppConfig? appConfig = config.GetRequiredSection("App").Get<AppConfig>();
                    if (appConfig == null)
                    {
                        throw new ConfigurationErrorsException(
                            "Configuration is missing or invalid."
                        );
                    }
                    Console.WriteLine("ServiceBusConnectionString: " + azureServiceBusConnectionString);

                    services.AddScoped<App.Providers.IBusProvider, App.Providers.BusProvider>();
                    services.AddScoped<App.Providers.IBlobProvider, App.Providers.BlobProvider>();
                    services.AddScoped<App.Services.ISimpleService, App.Services.SimpleService>();
                    services.AddSingleton(appConfig);

                    services.AddAzureClients(builder =>
                    {
                        builder.AddBlobServiceClient(azureStorageConnectionString);
                        builder.AddServiceBusClient(azureServiceBusConnectionString);
                    });
                });

            var app = host.Build();
            app.Run();
        }
    }
}
