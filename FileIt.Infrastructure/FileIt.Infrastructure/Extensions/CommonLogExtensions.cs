using System.Text;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

// A static class to hold the extension method
public static class CommonLogExtensions
{
    // Extension method for ILoggingBuilder
    public static ICommonLogConfig GetCommonLogConfig(this IConfiguration configuration)
    {
        ICommonLogConfig logConfig = new CommonLogConfig();
        if (string.IsNullOrWhiteSpace(logConfig.Environment))
        {
            string? azure_env = configuration.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT");
            logConfig.Environment = azure_env ?? logConfig.Environment;
        }
        logConfig.Host = Environment.MachineName;
        logConfig.Agent = Environment.UserName;
        logConfig.DbConnectionString =
            configuration.GetConnectionString("FileItDbConnection")
            ?? configuration.GetValue<string>("FileItDbConnection")
            ?? throw new ApplicationException("Connection Strings is missing FileItDbConnection.");

        string? logFilePath = configuration.GetValue<string>("LOG_FILE_PATH");
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            logConfig.LogFilePath = logFilePath;
        }

        string? serilogFilePath = configuration.GetValue<string>("SERILOG_SELFLOG_FILE_PATH");
        if (!string.IsNullOrWhiteSpace(serilogFilePath))
        {
            logConfig.SerilogSelfLogFilePath = serilogFilePath;
        }
        return logConfig;
    }

    public static ILoggingBuilder AddCommonLog(
        this ILoggingBuilder builder,
        ICommonLogConfig featureConfig,
        string? selfLogFilePath = null
    )
    {
        var temp = new StringBuilder();
        temp.Append("{{");
        temp.Append("\n\t\"@t\":\"{Timestamp:o}\",");
        temp.Append("\n\t\"@l\":\"{Level}\",");
        temp.Append("\n\t\"Message\":\"{Message:lj}\",");
        temp.Append("\n\t\"MachineName\":\"{MachineName}\",");
        temp.Append("\n\t\"Application\":\"{Application}\",");
        temp.Append("\n\t\"ApplicationVersion\":\"{ApplicationVersion}\",");
        temp.Append("\n\t\"InfrastructureVersion\":\"{InfrastructureVersion}\",");
        temp.Append("\n\t\"SourceContext\":\"{SourceContext}\",");
        temp.Append("\n\t\"CorrelationId\":\"{CorrelationId}\",");
        temp.Append("\n\t\"InvocationId\":\"{InvocationId}\",");
        temp.Append("\n\t\"EventId\": {EventId}");
        temp.Append("\n}}{NewLine}{Exception}");

        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Override("Azure", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Optional: control noise from built-in Microsoft logging
                .MinimumLevel.Debug()
                .WriteTo.DatabaseSink(featureConfig)
                .WriteTo.Console(outputTemplate: temp.ToString())
                .Enrich.With(new EventIdDimensionsEnricher())
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Application", featureConfig.Application)
                .Enrich.WithProperty("ApplicationVersion", featureConfig.ApplicationVersion)
                .Enrich.WithProperty(
                    "InfrastructureVersion",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                );

            if (!string.IsNullOrWhiteSpace(featureConfig.LogFilePath))
            {
                loggerConfig.WriteTo.File(featureConfig.LogFilePath, shared: true);
            }

#if RELEASE
            // Resolve TelemetryConfiguration from DI so the sink does not require a raw connection string.
            var telemetryConfiguration = serviceProvider.GetService<TelemetryConfiguration>();
            if (telemetryConfiguration != null)
            {
                loggerConfig.WriteTo.ApplicationInsights(
                    telemetryConfiguration,
                    TelemetryConverter.Traces
                );
            }
#endif

            var logger = loggerConfig.CreateLogger();
            Log.Logger = logger;
            return new SerilogLoggerProvider(logger, true);
        });

        // Use the preconfigured Serilog Logger (Log.Logger) for the Functions application.
        // FunctionsApplicationBuilder doesn't expose Host, so attach Serilog through the ILoggingBuilder.
        if (!string.IsNullOrWhiteSpace(selfLogFilePath))
        {
            Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText(selfLogFilePath, msg));
        }
        return builder;
    }
}
