using System.Configuration;
using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Integration;

public static class TestHost
{
    public static IHost CreateHost(Action<IServiceCollection>? configureServices = null)
    {
        var _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", false, false)
            .AddJsonFile(@"FileIt.Infrastructure.Integration.testconfig.json", false, false)
            .AddEnvironmentVariables()
            .Build();

        var builder = Host.CreateDefaultBuilder();

        var sectionName = _configuration.GetValue<string>("FeatureSection") ?? "Feature";
        IntegrationConfig? config = _configuration.GetSection(sectionName).Get<IntegrationConfig>();
        if (config == null)
        {
            throw new ApplicationException("Appsettings.json is missing Feature config.");
        }
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(config);

            IInfrastructureConfig infrastructureConfig = new InfrastructureConfig(_configuration);
            services.AddInfrastructure(infrastructureConfig);

            configureServices?.Invoke(services);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders(); // Remove default logging providers
            ICommonLogConfig logConfig = _configuration.GetCommonLogConfig();
            logConfig.ApplicationVersion = System
                .Reflection.Assembly.GetExecutingAssembly()
                .GetName()
                .Version?.ToString();
            logging.AddCommonLog(logConfig);
        });

        Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));
        return builder.Build();
    }

    public static async Task RunWithHost(
        Func<IServiceProvider, Task> testAction,
        Action<IServiceCollection>? configureServices = null
    )
    {
        using var host = CreateHost(configureServices);
        await host.StartAsync();

        try
        {
            await testAction(host.Services);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
