using System.Reflection;
using System.Text;
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights;

namespace FileIt.Infrastructure.Tools
{
    public class ProgramTool
    {
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
                builder.Configuration.GetValue<string>("DB_CONNECTION_STRING")
                ?? throw new ApplicationException(
                    "Application settings is missing DB_CONNECTION_STRING."
                );
            featureConfig.BusConnectionString =
                builder.Configuration.GetValue<string>("SERVICEBUS_CONNECTION_STRING")
                ?? throw new ApplicationException(
                    "Application settings is missing SERVICEBUS_CONNECTION_STRING."
                );
            featureConfig.BlobConnectionString =
                builder.Configuration.GetValue<string>("STORAGE_CONNECTION_STRING")
                ?? throw new ApplicationException(
                    "Application settings is missing STORAGE_CONNECTION_STRING."
                );
            featureConfig.AppInsightsConnectionString = builder.Configuration.GetValue<string>(
                "APPLICATIONINSIGHTS_CONNECTION_STRING"
            );

            if (string.IsNullOrWhiteSpace(featureConfig.BusNamespace))
            {
                throw new ApplicationException("Application settings is missing BusNamespace.");
            }

            string? logFilePath = builder.Configuration.GetValue<string>("LOG_FILE_PATH");
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                featureConfig.LogFilePath = logFilePath;
            }

            string? serilogFilePath = builder.Configuration.GetValue<string>(
                "SERILOG_SELFLOG_FILE_PATH"
            );
            if (!string.IsNullOrWhiteSpace(serilogFilePath))
            {
                featureConfig.SerilogSelfLogFilePath = serilogFilePath;
            }

            return featureConfig;
        }
    }
}
