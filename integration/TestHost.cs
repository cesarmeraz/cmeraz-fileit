using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using FileIt.App.Data;
using FileIt.App.Features.Simple;
using FileIt.App.Repositories;
using FileIt.App.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.MSSqlServer;

public static class TestHost
{
    public static IHost CreateHost(Action<IServiceCollection> configureServices = null)
    {
        var _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", false, false)
            .AddEnvironmentVariables()
            .Build();
        var connstring = _configuration.GetConnectionString("FileItDb");

        var builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                var sinkOpts = new MSSqlServerSinkOptions
                {
                    TableName = "GeneralAuditLog2",
                    AutoCreateSqlDatabase = false,
                    AutoCreateSqlTable = true,
                };
                var columnOpts = new ColumnOptions()
                {
                    AdditionalColumns = new Collection<SqlColumn>
                    {
                        new SqlColumn()
                        {
                            ColumnName = "EnvironmentName",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "MachineName",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "ApplicationName",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "Version",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "Module",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "SourceContext",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "ClientRequestId",
                            DataType = SqlDbType.NVarChar,
                            DataLength = 100,
                            AllowNull = true,
                        },
                        new SqlColumn()
                        {
                            ColumnName = "EventId",
                            DataType = SqlDbType.Int,
                            AllowNull = true,
                        },
                    },
                };
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.MSSqlServer(
                        connectionString: _configuration.GetConnectionString("FileItDb"),
                        sinkOptionsSection: _configuration.GetSection("sinkOptionsSection"),
                        columnOptionsSection: _configuration.GetSection("columnOptionsSection"),
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose
                    )
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
                logging.AddSerilog(Log.Logger);
            })
            .ConfigureServices(
                (hostContext, services) =>
                {
                    ConfigTool? appConfig = _configuration
                        .GetRequiredSection("App")
                        .Get<ConfigTool>();
                    if (appConfig == null)
                    {
                        throw new ConfigurationErrorsException(
                            "Configuration is missing or invalid."
                        );
                    }
                    string connstring =
                        _configuration.GetConnectionString("FileItDb")
                        ?? throw new ConfigurationErrorsException(
                            "FileItDb Connection string is missing."
                        );

                    // Register your application's services here for testing
                    services.AddSingleton(appConfig.Api);
                    services.AddSingleton(appConfig.Common);
                    services.AddSingleton(appConfig.Simple);
                    services.AddSingleton<IApiLogRepo, ApiLogRepo>();
                    services.AddSingleton<ISimpleRequestLogRepo, SimpleRequestLogRepo>();
                    services.AddDbContextFactory<AppDbContext>(options =>
                        options.UseSqlServer(connstring)
                    );

                    // Allow external configuration of services for specific tests
                    configureServices?.Invoke(services);
                }
            );
        Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));
        return builder.Build();
    }

    public static async Task RunWithHost(
        Func<IServiceProvider, Task> testAction,
        Action<IServiceCollection> configureServices = null
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
