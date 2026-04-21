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

        // Ship logs to Aspire dashboard via OTLP when running under Aspire.
        // OTEL_EXPORTER_OTLP_ENDPOINT is auto-injected by Aspire into each child process.
        // If the variable is absent (standalone func run), this block is skipped and behavior is unchanged.
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            // Enable Serilog SelfLog to stderr so we can see sink errors in the Aspire console
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            // Aspire ships OTLP over https with a dev cert on localhost.
            // Bypass cert validation on the underlying HttpClient for local dev.
            var httpHandler = new System.Net.Http.HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                options.HttpMessageHandler = httpHandler;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = featureConfig.Application ?? "FileIt",
                    ["service.version"] = featureConfig.ApplicationVersion ?? "1.0.0"
                };
            });
        }

#if RELEASE
        loggerConfig.WriteTo.ApplicationInsights(
            featureConfig.AppInsightsConnectionString,
            TelemetryConverter.Traces
        );
#endif

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
