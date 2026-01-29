// Extensions/ServiceCollectionExtensions.cs
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.DependencyInjection;
using FileIt.Infrastructure.Tools;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace FileIt.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IInfrastructureConfig GetInfrastructureConfig(
        this FunctionsApplicationBuilder builder
    )
    {
        IInfrastructureConfig config = new InfrastructureConfig();

        config.DbConnectionString =
            builder.Configuration.GetValue<string>("DB_CONNECTION_STRING")
            ?? throw new ApplicationException(
                "Application settings is missing DB_CONNECTION_STRING."
            );
        config.BusConnectionString =
            builder.Configuration.GetValue<string>("SERVICEBUS_CONNECTION_STRING")
            ?? throw new ApplicationException(
                "Application settings is missing SERVICEBUS_CONNECTION_STRING."
            );
        config.BlobConnectionString =
            builder.Configuration.GetValue<string>("STORAGE_CONNECTION_STRING")
            ?? throw new ApplicationException(
                "Application settings is missing STORAGE_CONNECTION_STRING."
            );
        config.AppInsightsConnectionString = builder.Configuration.GetValue<string>(
            "APPLICATIONINSIGHTS_CONNECTION_STRING"
        );
        config.BusNamespace =
            builder.Configuration.GetValue<string>("SERVICEBUS_NAMESPACE")
            ?? throw new ApplicationException(
                "Application settings is missing SERVICEBUS_NAMESPACE."
            );

        return config;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IInfrastructureConfig config
    )
    {
        if (config == null)
        {
            throw new ArgumentNullException("Missing config.");
        }
        if (string.IsNullOrWhiteSpace(config.DbConnectionString))
        {
            throw new ArgumentException("Config is missing DbConnectionString");
        }

        // Register your services here
        services.AddSingleton(config);
        services.AddScoped<ITalkToApi, BusTool>();
        services.AddScoped<IHandleFiles, BlobTool>();
        services.AddScoped<IApiLogRepo, ApiLogRepo>();
        services.AddScoped<ISimpleRequestLogRepo, SimpleRequestLogRepo>();
        services.AddDbContextFactory<CommonDbContext>(options =>
            options.UseSqlServer(config.DbConnectionString)
        );

        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddBlobServiceClient(config.BlobConnectionString);
            clientBuilder.AddServiceBusClient(config.BusConnectionString);
            clientBuilder.AddServiceBusAdministrationClientWithNamespace(config.BusNamespace);

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
                            .CreateSender("api-add-topic")
                )
                .WithName("api-add-topic");

            // Set a credential for all clients to use by default
            // DefaultAzureCredential credential = new();
            // clientBuilder.UseCredential(credential);
        });

        services.AddSingleton<ILoggerProvider>(new SerilogLoggerProvider(Log.Logger));

        return services;
    }
}
