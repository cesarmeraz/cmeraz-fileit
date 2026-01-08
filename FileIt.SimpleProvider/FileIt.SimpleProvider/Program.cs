using System.Configuration;
using System.Text;
using Azure.Messaging.ServiceBus;
using FileIt.Common.Data;
using FileIt.Common.Domain;
using FileIt.Common.Tools;
using FileIt.SimpleProvider;
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
using Serilog.Extensions.Hosting;
using Serilog.Extensions.Logging;
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

SimpleConfig? featureConfig = programTool.GetFeatureConfig<SimpleConfig>(builder.Configuration);
featureConfig.FeatureVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();

builder.Services.AddScoped<IBusTool, BusTool>();
builder.Services.AddScoped<IBlobTool, BlobTool>();
builder.Services.AddScoped<ISimpleRequestLogRepo, SimpleRequestLogRepo>();
builder.Services.AddSingleton(featureConfig);
string connstring =
    config.GetConnectionString("FileItDb")
    ?? throw new ConfigurationErrorsException("FileItDb Connection string is missing.");

builder.Services.AddDbContextFactory<SimpleDbContext>(options => options.UseSqlServer(connstring));
builder.Services.AddDbContextFactory<CommonDbContext>(options => options.UseSqlServer(connstring));
builder.Services.AddSingleton<ILoggerProvider>(new SerilogLoggerProvider(Log.Logger));
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
builder.Logging.AddSerilog(Log.Logger, true);

// Use the preconfigured Serilog Logger (Log.Logger) for the Functions application.
// FunctionsApplicationBuilder doesn't expose Host, so attach Serilog through the ILoggingBuilder.
Serilog.Debugging.SelfLog.Enable(msg =>
    File.AppendAllText("/home/cesar/repos/cmeraz-fileit/FileIt.SimpleProvider/serilog.txt", msg)
);

builder.Build().Run();
