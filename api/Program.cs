using System.Configuration;
using FileIt.App.Models;
using Microsoft.Azure.Functions.Worker;
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

            string azureFunctionsEnvironment = config.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? string.Empty;
            string azureStorageConnectionString = config.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING") ?? string.Empty;
            string azureServiceBusConnectionString = config.GetValue<string>("ServiceBus") ?? string.Empty;

            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication() // This line is crucial for ASP.NET Core Integration
                //        .ConfigureFunctionsWorkerDefaults() // Configures default settings for the isolated worker
                .ConfigureServices(services =>
                {
                    //bind Configuration to AppConfig
                    AppConfig? appConfig = config.GetRequiredSection("App").Get<AppConfig>();

                    if (appConfig == null)
                    {
                        throw new ConfigurationErrorsException(
                            "Configuration is missing or invalid."
                        );
                    }
                    Console.WriteLine("ServiceBusConnectionString: " + azureServiceBusConnectionString);

                    // Register your services here for dependency injection
                    // Example: services.AddSingleton<IMyService, MyService>();
                    services.AddScoped<App.Providers.IBusProvider, App.Providers.BusProvider>();
                    services.AddScoped<App.Providers.IBlobProvider, App.Providers.BlobProvider>();
                    services.AddScoped<App.Services.ISimpleService, App.Services.SimpleService>();
                    services.AddSingleton(appConfig);

                    // Add any other necessary services or configurations
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
