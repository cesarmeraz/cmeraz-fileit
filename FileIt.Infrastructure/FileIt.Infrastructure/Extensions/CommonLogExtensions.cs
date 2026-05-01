using System.Text;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.TextFormatters;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting;
using Serilog.Sinks.File.Header;
using Serilog.Templates;

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
        ICommonLogConfig commonLogConfig,
        string? selfLogFilePath = null
    )
    {
        string logFormatterKey = commonLogConfig.LogFormatterKey ?? "json";
        string logExpression = commonLogConfig.LogExpression ?? string.Empty;
        var defaultExpression = new StringBuilder();
        defaultExpression.Append("{ {");
        defaultExpression.Append("\n\t\"Timestamp\":\"{Timestamp:o}\",");
        defaultExpression.Append("\n\t\"Level\":\"{Level}\",");
        defaultExpression.Append("\n\t\"Message\":\"{Message:lj}\",");
        defaultExpression.Append("\n\t\"MachineName\":\"{MachineName}\",");
        defaultExpression.Append("\n\t\"Application\":\"{Application}\",");
        defaultExpression.Append("\n\t\"ApplicationVersion\":\"{ApplicationVersion}\",");
        defaultExpression.Append("\n\t\"InfrastructureVersion\":\"{InfrastructureVersion}\",");
        defaultExpression.Append("\n\t\"SourceContext\":\"{SourceContext}\",");
        defaultExpression.Append("\n\t\"CorrelationId\":\"{CorrelationId}\",");
        defaultExpression.Append("\n\t\"InvocationId\":\"{InvocationId}\",");
        defaultExpression.Append("\n\t\"EventId\": {EventId}");
        defaultExpression.Append("\n\t\"Exception\": {Exception}");
        defaultExpression.Append("\n} }");

        builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
        {
            IFileItTextFormatter? textFormatter =
                serviceProvider.GetKeyedService<IFileItTextFormatter>(logFormatterKey);
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Override("Azure", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Optional: control noise from built-in Microsoft logging
                .MinimumLevel.Debug()
                .WriteTo.DatabaseSink(commonLogConfig)
                .Enrich.With(new EventIdDimensionsEnricher())
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Application", commonLogConfig.Application)
                .Enrich.WithProperty("ApplicationVersion", commonLogConfig.ApplicationVersion)
                .Enrich.WithProperty(
                    "InfrastructureVersion",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                );

            if (!string.IsNullOrWhiteSpace(commonLogConfig.LogFilePath))
            {
                if (string.IsNullOrWhiteSpace(logExpression))
                {
                    string extension = textFormatter?.GetFileExtension() ?? "log";
                    string header = textFormatter?.GetHeader() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        loggerConfig.WriteTo.File(
                            textFormatter ?? new FileItJsonFormatter(),
                            $"{commonLogConfig.LogFilePath}/log.{extension}",
                            shared: true,
                            rollingInterval: RollingInterval.Day, // Rolls every day
                            fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB limit
                            rollOnFileSizeLimit: true, // Create new file when limit reached
                            retainedFileCountLimit: 31 // Keep 31 files max
                        );
                    }
                    else
                    {
                        loggerConfig.WriteTo.File(
                            textFormatter ?? new FileItCsvFormatter(),
                            $"{commonLogConfig.LogFilePath}/log.{extension}",
                            shared: true,
                            rollingInterval: RollingInterval.Day, // Rolls every day
                            fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB limit
                            rollOnFileSizeLimit: true, // Create new file when limit reached
                            retainedFileCountLimit: 31, // Keep 31 files max
                            hooks: new HeaderWriter("Timestamp,Level,Message")
                        );
                    }
                }
                else
                {
                    loggerConfig.WriteTo.File(
                        new ExpressionTemplate(logExpression),
                        commonLogConfig.LogFilePath,
                        shared: true,
                        rollingInterval: RollingInterval.Day, // Rolls every day
                        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB limit
                        rollOnFileSizeLimit: true, // Create new file when limit reached
                        retainedFileCountLimit: 31 // Keep 31 files max
                    );
                }
            }
#if DEBUG
            if (string.IsNullOrWhiteSpace(logExpression))
            {
                loggerConfig
                    .MinimumLevel.Debug()
                    .WriteTo.Console(textFormatter ?? new FileItJsonFormatter());
            }
            else
            {
                loggerConfig
                    .MinimumLevel.Debug()
                    .WriteTo.Console(new ExpressionTemplate(logExpression));
            }
#endif
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
