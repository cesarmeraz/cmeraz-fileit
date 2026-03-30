using System.Text;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;

// A static class to hold the extension method
public static class CommonLogExtensions
{
    // Extension method for ILoggingBuilder
    public static ICommonLogConfig GetCommonLogConfig(this FunctionsApplicationBuilder builder)
    {
        ICommonLogConfig config = new CommonLogConfig();
        if (string.IsNullOrWhiteSpace(config.Environment))
        {
            string? azure_env = builder.Configuration.GetValue<string>(
                "AZURE_FUNCTIONS_ENVIRONMENT"
            );
            config.Environment = azure_env ?? config.Environment;
        }
        config.Host = Environment.MachineName;
        config.Agent = Environment.UserName;
        config.Environment = builder.Environment.EnvironmentName;
        config.Application = builder.Environment.ApplicationName;
        config.DbConnectionString =
            builder.Configuration.GetConnectionString("FileItDbConnection")
            ?? builder.Configuration.GetValue<string>("FileItDbConnection")
            ?? throw new ApplicationException("Connection Strings is missing FileItDbConnection.");

        string? logFilePath = builder.Configuration.GetValue<string>("LOG_FILE_PATH");
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            config.LogFilePath = logFilePath;
        }

        string? serilogFilePath = builder.Configuration.GetValue<string>(
            "SERILOG_SELFLOG_FILE_PATH"
        );
        if (!string.IsNullOrWhiteSpace(serilogFilePath))
        {
            config.SerilogSelfLogFilePath = serilogFilePath;
        }
        builder.Services.AddSingleton(config);
        return config;
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

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Override("Azure", LogEventLevel.Warning)
            .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Optional: control noise from built-in Microsoft logging
            .MinimumLevel.Debug()
            .WriteTo.DatabaseSink(featureConfig)
            .WriteTo.Console(outputTemplate: temp.ToString())
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
            loggerConfig.WriteTo.File(featureConfig.LogFilePath);
        }
        if (featureConfig.Environment != "LocalDev")
        {
            loggerConfig.WriteTo.ApplicationInsights(
                featureConfig.AppInsightsConnectionString,
                TelemetryConverter.Traces
            );
        }
        Log.Logger = loggerConfig.CreateLogger();

        // Register your custom provider
        builder.AddSerilog(Log.Logger, true);

        // Use the preconfigured Serilog Logger (Log.Logger) for the Functions application.
        // FunctionsApplicationBuilder doesn't expose Host, so attach Serilog through the ILoggingBuilder.
        if (!string.IsNullOrWhiteSpace(selfLogFilePath))
        {
            Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText(selfLogFilePath, msg));
        }
        return builder;
    }
}
