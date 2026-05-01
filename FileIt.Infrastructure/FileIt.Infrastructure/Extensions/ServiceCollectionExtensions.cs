// Extensions/ServiceCollectionExtensions.cs
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Tools;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileIt.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IInfrastructureConfig GetInfrastructureConfig(
        this FunctionsApplicationBuilder builder
    )
    {
        IInfrastructureConfig config = new InfrastructureConfig(builder.Configuration);
        return config;
    }

    /// <summary>
    /// Registers the IBroadcastResponses interface and its implementation PublishTool in the
    /// dependency injection container. Using an extension method allows the system to keep
    /// the Function App decoupled from the Domain, where the interface is defined.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddIBroadcastResponses(this IServiceCollection services)
    {
        services.AddScoped<IBroadcastResponses, PublishTool>();
        return services;
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
            // Set a credential for all clients to use by default

            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            if (string.IsNullOrEmpty(clientId))
            {
                // LOCAL SETTINGS
                clientBuilder.AddBlobServiceClient(config.BlobConnectionString);
                clientBuilder.AddServiceBusClient(config.BusConnectionString);
                clientBuilder.AddServiceBusAdministrationClientWithNamespace(
                    config.BusConnectionString
                );
            }
            else
            {
                // PRODUCTION/AZURE: User Defined Managed Identity in Application Settings
                var blobStorage =
                    Environment.GetEnvironmentVariable("FileItStorage__serviceUri")
                    ?? throw new ApplicationException(
                        "Please set FileItStorage__serviceUri in Application settings."
                    );
                var serviceUri = new Uri(blobStorage);

                clientBuilder.AddBlobServiceClient(serviceUri);

                var serviceBusFullyQualifiedNamespaceKey =
                    "FileItServiceBus__fullyQualifiedNamespace";
                var namespaceName =
                    Environment.GetEnvironmentVariable(serviceBusFullyQualifiedNamespaceKey)
                    ?? throw new ApplicationException(
                        $"Please set {serviceBusFullyQualifiedNamespaceKey} in Application settings."
                    );

                clientBuilder.AddServiceBusClientWithNamespace(namespaceName);
                clientBuilder.AddServiceBusAdministrationClientWithNamespace(namespaceName);

                DefaultAzureCredential credential = new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId }
                );
                clientBuilder.UseCredential(credential);
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
        });

        return services;
    }
}
