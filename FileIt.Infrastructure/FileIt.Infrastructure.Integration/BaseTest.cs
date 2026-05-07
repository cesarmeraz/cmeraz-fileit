using System.Configuration;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileIt.Infrastructure.Integration;

public abstract class BaseTest
{
    protected readonly IConfiguration _configuration;
    protected ServiceProvider ServiceProvider;

    public BaseTest()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", false, false)
            .AddJsonFile(@"FileIt.Infrastructure.Integration.testconfig.json", false, false)
            .AddEnvironmentVariables()
            .Build();
        var services = new ServiceCollection();

        IntegrationConfig? appConfig = _configuration
            .GetRequiredSection("Feature")
            .Get<IntegrationConfig>();
        if (appConfig == null)
        {
            throw new ConfigurationErrorsException("Configuration is missing or invalid.");
        }
        string connstring =
            _configuration.GetConnectionString("DbConnectionString")
            ?? throw new ConfigurationErrorsException(
                "DbConnectionString Connection string is missing."
            );
        Console.WriteLine($"Using connection string: {connstring}");
        // Register services here
        services.AddSingleton(appConfig);
        services.AddScoped<IApiLogRepo, ApiLogRepo>();
        services.AddDbContextFactory<CommonDbContext>(options => options.UseSqlServer(connstring));
        ServiceProvider = services.BuildServiceProvider();
    }
}
