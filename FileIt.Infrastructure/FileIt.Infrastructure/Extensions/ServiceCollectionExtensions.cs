// Extensions/ServiceCollectionExtensions.cs
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace FileIt.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IFeatureConfig config
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

            foreach (var queueOrTopicName in config.QueueOrTopicNames)
            {
                clientBuilder
                    .AddClient<ServiceBusSender, ServiceBusClientOptions>(
                        (_, _, provider) =>
                            provider
                                .GetRequiredService<ServiceBusClient>()
                                .CreateSender(queueOrTopicName)
                    )
                    .WithName(queueOrTopicName);
            }

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

        services.AddSingleton<ILoggerProvider>(new SerilogLoggerProvider(Log.Logger));

        return services;
    }
}
