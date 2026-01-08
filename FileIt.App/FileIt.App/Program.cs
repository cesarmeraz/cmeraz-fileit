using System.Configuration;
using System.Text;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FileIt.App;
using FileIt.Common.Data;
using FileIt.Common.Tools;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var programTool = new ProgramTool();
IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    // .AddJsonFile("local.settings.json", true, false)
    .Build();

AppConfig? featureConfig = programTool.GetFeatureConfig<AppConfig>(builder.Configuration);
featureConfig.FeatureVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();

// wire up common items
string connstring =
    config.GetConnectionString("FileItDb")
    ?? throw new ConfigurationErrorsException("FileItDb Connection string is missing.");

// FEATURES
builder.Services.AddSingleton<IApiLogRepo, ApiLogRepo>();
builder.Services.AddSingleton(featureConfig);

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connstring));
builder.Services.AddSingleton<ILoggerProvider>(
    new Serilog.Extensions.Logging.SerilogLoggerProvider(Log.Logger)
);
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(config.GetConnectionString("Storage"));
    clientBuilder.AddServiceBusClient(config.GetConnectionString("ServiceBus"));
    clientBuilder.AddServiceBusAdministrationClientWithNamespace(featureConfig.BusNamespace);
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
                provider.GetRequiredService<ServiceBusClient>().CreateSender("api-add-topic")
        )
        .WithName("api-add-topic");
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

// Configure logging
programTool.InitLog(featureConfig);
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
