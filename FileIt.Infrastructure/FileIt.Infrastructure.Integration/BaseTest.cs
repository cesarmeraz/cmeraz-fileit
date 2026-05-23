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
        var basePath = Directory.GetCurrentDirectory();

        _configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("testconfig.json", optional: true, reloadOnChange: false)
            .AddInMemoryCollection(ParseDotEnvFile(Path.Combine(basePath, ".env")))
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

    private static IEnumerable<KeyValuePair<string, string?>> ParseDotEnvFile(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var idx = trimmed.IndexOf('=');
            if (idx <= 0)
                continue;

            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();

            bool isDoubleQuoted = value.Length >= 2 && value[0] == '"' && value[^1] == '"';
            bool isSingleQuoted = value.Length >= 2 && value[0] == '\'' && value[^1] == '\'';
            if (isDoubleQuoted || isSingleQuoted)
            {
                value = value[1..^1];
            }

            yield return new KeyValuePair<string, string?>(key, value);
        }
    }
}
