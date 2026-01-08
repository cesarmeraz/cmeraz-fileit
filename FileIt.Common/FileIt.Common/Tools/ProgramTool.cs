using System.Reflection;
using System.Text;
using FileIt.Common.Domain;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights;

namespace FileIt.Common.Tools
{
    public class ProgramTool
    {
        public IConfigurationRoot GetConfigRoot(string environmentName)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                // .AddJsonFile("local.settings.json", true, false)
                .Build();
        }

        public T GetFeatureConfig<T>(IConfigurationRoot config)
            where T : IFeatureConfig
        {
            var sectionName = config.GetValue<string>("FeatureSection") ?? "Feature";
            var featureConfig = config.GetSection(sectionName).Get<T>();
            if (featureConfig == null)
            {
                throw new ApplicationException("Appsettings.json is missing Feature config.");
            }
            if (string.IsNullOrWhiteSpace(featureConfig.Environment))
            {
                string? azure_env = config.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT");
                featureConfig.Environment = azure_env ?? featureConfig.Environment;
            }
            featureConfig.Host = Environment.MachineName;
            featureConfig.Agent = Environment.UserName;
            featureConfig.DbConnectionString =
                config.GetConnectionString("FileItDb")
                ?? throw new ApplicationException(
                    "Application settings is missing DB_CONNECTION_STRING."
                );
            featureConfig.AppInsightsConnectionString = config.GetValue<string>(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            );

            string? logFilePath = config.GetValue<string>("LOG_FILE_PATH");
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                featureConfig.LogFilePath = logFilePath;
            }
            return featureConfig;
        }

        public T GetFeatureConfig<T>(FunctionsApplicationBuilder builder)
            where T : IFeatureConfig
        {
            var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
            var featureConfig = builder.Configuration.GetSection(sectionName).Get<T>();
            if (featureConfig == null)
            {
                throw new ApplicationException("Appsettings.json is missing Feature config.");
            }
            if (string.IsNullOrWhiteSpace(featureConfig.Environment))
            {
                string? azure_env = builder.Configuration.GetValue<string>(
                    "AZURE_FUNCTIONS_ENVIRONMENT"
                );
                featureConfig.Environment = azure_env ?? featureConfig.Environment;
            }
            featureConfig.Host = Environment.MachineName;
            featureConfig.Agent = Environment.UserName;
            featureConfig.Environment = builder.Environment.EnvironmentName;
            featureConfig.Feature = builder.Environment.ApplicationName;
            featureConfig.DbConnectionString =
                builder.Configuration.GetConnectionString("FileItDb")
                ?? throw new ApplicationException(
                    "Application settings is missing DB_CONNECTION_STRING."
                );
            featureConfig.AppInsightsConnectionString = builder.Configuration.GetValue<string>(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            );

            string? logFilePath = builder.Configuration.GetValue<string>("LOG_FILE_PATH");
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                featureConfig.LogFilePath = logFilePath;
            }
            return featureConfig;
        }

        public void AddServices(FunctionsApplicationBuilder builder)
        {
            builder.Services.AddScoped<IBusTool, BusTool>();
            builder.Services.AddScoped<IBlobTool, BlobTool>();
        }

        public void InitLog(IFeatureConfig featureConfig)
        {
            var temp = new StringBuilder();
            temp.Append("{{");
            temp.Append("\n\t\"@t\":\"{Timestamp:o}\",");
            temp.Append("\n\t\"@l\":\"{Level}\",");
            temp.Append("\n\t\"Message\":\"{Message:lj}\",");
            temp.Append("\n\t\"MachineName\":\"{MachineName}\",");
            temp.Append("\n\t\"ApplicationName\":\"{ApplicationName}\",");
            temp.Append("\n\t\"Feature\":\"{Feature}\",");
            temp.Append("\n\t\"FeatureVersion\":\"{FeatureVersion}\",");
            temp.Append("\n\t\"CommonVersion\":\"{CommonVersion}\",");
            temp.Append("\n\t\"SourceContext\":\"{SourceContext}\",");
            temp.Append("\n\t\"ClientRequestId\":\"{ClientRequestId}\",");
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
        }
    }
}
