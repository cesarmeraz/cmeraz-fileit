using System.Text;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;

// A static class to hold the extension method
public static class CommonLoggerExtensions
{
    // Extension method for ILoggingBuilder
    public static ILoggingBuilder AddCommonLogger(
        this ILoggingBuilder builder,
        IFeatureConfig featureConfig,
        string? selfLogFilePath = null
    )
    {
        var temp = new StringBuilder();
        temp.Append("{{");
        temp.Append("\n\t\"@t\":\"{Timestamp:o}\",");
        temp.Append("\n\t\"@l\":\"{Level}\",");
        temp.Append("\n\t\"Message\":\"{Message:lj}\",");
        temp.Append("\n\t\"MachineName\":\"{MachineName}\",");
        temp.Append("\n\t\"Feature\":\"{Feature}\",");
        temp.Append("\n\t\"FeatureVersion\":\"{FeatureVersion}\",");
        temp.Append("\n\t\"CommonVersion\":\"{CommonVersion}\",");
        temp.Append("\n\t\"SourceContext\":\"{SourceContext}\",");
        temp.Append("\n\t\"CorrelationId\":\"{CorrelationId}\",");
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
            .Enrich.WithProperty("Feature", featureConfig.Feature)
            .Enrich.WithProperty("FeatureVersion", featureConfig.FeatureVersion)
            .Enrich.WithProperty(
                "CommonVersion",
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
