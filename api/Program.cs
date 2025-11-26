using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FileIt.App.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileIt.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            bool isInCodespace = config.GetValue<bool>("CODESPACES", false);
            string hostName = isInCodespace
                ? config.GetValue<string>("CODESPACE_NAME") ?? "codespace"
                : config.GetValue<string>("WEBSITE_HOSTNAME") ?? "unknown-host";
            string agent = isInCodespace
                ? config.GetValue<string>("GITHUB_USER") ?? "codespace"
                : config.GetValue<string>("WEBSITE_SITE_NAME") ?? "unknown-agent";
            string azureFunctionsEnvironment =
                config.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? string.Empty;
            string azureStorageConnectionString =
                config.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING") ?? string.Empty;
            string azureServiceBusConnectionString =
                config.GetValue<string>("ServiceBus") ?? string.Empty;

            var builder = FunctionsApplication.CreateBuilder(args);
            builder.ConfigureFunctionsWebApplication(); // This line is crucial for ASP.NET Core Integration
            builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
            {
                // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
                // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/azure/azure-monitor/app/worker-service#ilogger-logs
                LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule =>
                    rule.ProviderName
                    == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider"
                );
                if (defaultRule is not null)
                {
                    options.Rules.Remove(defaultRule);
                }
            });
            AppConfig? appConfig = config.GetRequiredSection("App").Get<AppConfig>();
            if (appConfig == null)
            {
                throw new ConfigurationErrorsException("Configuration is missing or invalid.");
            }
            appConfig.Environment = azureFunctionsEnvironment;
            appConfig.Host = hostName;
            appConfig.Agent = agent;

            Console.WriteLine("ServiceBusConnectionString: " + azureServiceBusConnectionString);

            builder.Services.AddScoped<App.Providers.IBusProvider, App.Providers.BusProvider>();
            builder.Services.AddScoped<App.Providers.IBlobProvider, App.Providers.BlobProvider>();
            builder.Services.AddScoped<App.Services.ISimpleService, App.Services.SimpleService>();
            builder.Services.AddSingleton(appConfig);

            builder.Services.AddAzureClients(async clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(azureStorageConnectionString);
                clientBuilder.AddServiceBusClient(azureServiceBusConnectionString);
                clientBuilder.AddServiceBusAdministrationClientWithNamespace(
                    appConfig.ServiceBusNamespace
                );

                // Set a credential for all clients to use by default
                DefaultAzureCredential credential = new();
                clientBuilder.UseCredential(credential);

                // Register a subclient for each Service Bus Queue
                List<string> queueNames = await GetQueueNames(
                    credential,
                    appConfig.ServiceBusNamespace
                );
                foreach (string queue in queueNames)
                {
                    _ = clientBuilder
                        .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                            (_, _, provider) =>
                                provider.GetRequiredService<ServiceBusClient>().CreateSender(queue)
                        )
                        .WithName(queue);
                }
            });

            var app = builder.Build();
            app.Run();
        }

        public static async Task<List<string>> GetQueueNames(
            DefaultAzureCredential credential,
            string serviceBusNamespace
        )
        {
            // Query the available queues for the Service Bus namespace.
            var adminClient = new ServiceBusAdministrationClient(serviceBusNamespace, credential);
            var queueNames = new List<string>();

            // Because the result is async, the queue names need to be captured
            // to a standard list to avoid async calls when registering. Failure to
            // do so results in an error with the services collection.
            await foreach (QueueProperties queue in adminClient.GetQueuesAsync())
            {
                Console.WriteLine($"Found queue: {queue.Name}");
                queueNames.Add(queue.Name);
            }

            return queueNames;
        }
    }
}
