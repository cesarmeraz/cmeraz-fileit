using System.Configuration;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FileIt.App.Api;
using FileIt.App.Data;
using FileIt.App.Models;
using FileIt.App.Providers;
using FileIt.App.Repositories;
using FileIt.App.Simple;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FileIt.App
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
                config.GetConnectionString("Storage") ?? string.Empty;
            string azureServiceBusConnectionString =
                config.GetConnectionString("ServiceBus") ?? string.Empty;

            var builder = FunctionsApplication.CreateBuilder(args);
            builder.ConfigureFunctionsWebApplication();

            if (isProduction)
            {
                builder
                    .Services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();
            }

            AppConfig? appConfig = config.GetRequiredSection("App").Get<AppConfig>();
            if (appConfig == null)
            {
                throw new ConfigurationErrorsException("Configuration is missing or invalid.");
            }
            appConfig.Environment = azureFunctionsEnvironment;
            appConfig.Host = hostName;
            appConfig.Agent = agent;

            Console.WriteLine("ServiceBusConnectionString: " + azureServiceBusConnectionString);

            builder.Services.AddScoped<IBusProvider, BusProvider>();
            builder.Services.AddScoped<IBlobProvider, BlobProvider>();
            builder.Services.AddSingleton(appConfig);
            string connstring =
                config.GetConnectionString("FileItDb")
                ?? throw new ConfigurationErrorsException("FileItDb Connection string is missing.");

            // Register your application's services here for testing
            // services.AddTransient<IMyService, MyService>();
            builder.Services.AddSingleton(appConfig);
            builder.Services.AddSingleton<IApiLogRepo, ApiLogRepo>();
            builder.Services.AddSingleton<ISimpleRequestLogRepo, SimpleRequestLogRepo>();
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(connstring)
            );
            builder.Services.AddSingleton<ILoggerProvider>(
                new Serilog.Extensions.Logging.SerilogLoggerProvider(Log.Logger)
            );

            builder.Services.AddAzureClients(async clientBuilder =>
            {
                clientBuilder.AddBlobServiceClient(azureStorageConnectionString);
                clientBuilder.AddServiceBusClient(azureServiceBusConnectionString);
                clientBuilder.AddServiceBusAdministrationClientWithNamespace(
                    appConfig.ServiceBusNamespace
                );
                clientBuilder
                    .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                        (_, _, provider) =>
                            provider.GetRequiredService<ServiceBusClient>().CreateSender("simple")
                    )
                    .WithName("simple");
                clientBuilder
                    .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                        (_, _, provider) =>
                            provider.GetRequiredService<ServiceBusClient>().CreateSender("api-add")
                    )
                    .WithName("api-add");
                clientBuilder
                    .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                        (_, _, provider) =>
                            provider
                                .GetRequiredService<ServiceBusClient>()
                                .CreateSender("api-add-simple")
                    )
                    .WithName("api-add-simple");

                // Set a credential for all clients to use by default
                // DefaultAzureCredential credential = new();
                // clientBuilder.UseCredential(credential);

                // // Register a subclient for each Service Bus Queue
                // List<string> queueNames = await GetQueueNames(
                //     credential,
                //     appConfig.ServiceBusNamespace
                // );
                // foreach (string queue in queueNames)
                // {
                //     _ = clientBuilder
                //         .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                //             (_, _, provider) =>
                //                 provider.GetRequiredService<ServiceBusClient>().CreateSender(queue)
                //         )
                //         .WithName(queue);
                // }
            });

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Azure", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Optional: control noise from built-in Microsoft logging
                .MinimumLevel.Debug()
                .WriteTo.File("/home/cesar/repos/cmeraz-fileit/app/log.txt")
                .WriteTo.Console()
                .WriteTo.MSSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("FileItDb"),
                    sinkOptionsSection: builder.Configuration.GetSection("sinkOptionsSection"),
                    columnOptionsSection: builder.Configuration.GetSection("columnOptionsSection"),
                    restrictedToMinimumLevel: LogEventLevel.Debug
                )
                // .WriteTo.ApplicationInsights( // If applicable to Azure Functions
                //     context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                //     TelemetryConverter.Traces
                // )
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProperty(
                    "ApplicationName",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name
                )
                .Enrich.WithProperty(
                    "Version",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                )
                .CreateLogger();

            // Configure logging
            builder.Logging.ClearProviders(); // Remove default logging providers
            builder.Logging.AddConsole();
            builder.Logging.AddSerilog(Log.Logger, true);

            // Use the preconfigured Serilog Logger (Log.Logger) for the Functions application.
            // FunctionsApplicationBuilder doesn't expose Host, so attach Serilog through the ILoggingBuilder.
            Serilog.Debugging.SelfLog.Enable(msg =>
                File.AppendAllText("/home/cesar/repos/cmeraz-fileit/app/serilog.txt", msg)
            );

            // builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
            // {
            //     // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
            //     // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/azure/azure-monitor/app/worker-service#ilogger-logs
            //     LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule =>
            //         rule.ProviderName
            //         == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider"
            //     );
            //     if (defaultRule is not null)
            //     {
            //         options.Rules.Remove(defaultRule);
            //     }
            // });

            var app = builder.Build();
            try
            {
                await app.RunAsync();
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
