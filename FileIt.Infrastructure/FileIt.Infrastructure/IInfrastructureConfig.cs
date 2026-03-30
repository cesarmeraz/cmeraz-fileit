using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure;

public interface IInfrastructureConfig
{
    string? AppInsightsConnectionString { get; set; }
    string? BusNamespace { get; set; }
    string? DbConnectionString { get; set; }
    string? Environment { get; set; }
    string? Feature { get; set; }
    string? BlobConnectionString { get; set; }
    string? BusConnectionString { get; set; }
}

public class InfrastructureConfig : IInfrastructureConfig
{
    private ConfigurationManager? configuration;

    public InfrastructureConfig() { }

    private const string SERVICEBUS_NAMESPACE = "SERVICEBUS_NAMESPACE";
    private const string DB_CONNECTION_STRING = "FileItDbConnection";
    private const string SERVICEBUS_CONNECTION_STRING = "FileItServiceBus";
    private const string STORAGE_CONNECTION_STRING = "FileItStorage";
    private const string APPLICATIONINSIGHTS_CONNECTION_STRING =
        "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public InfrastructureConfig(ConfigurationManager configuration)
    {
        this.configuration = configuration;
        List<string> missing = new List<string>();
        string? parsedValue;
        if (ParseConfigValue(SERVICEBUS_NAMESPACE, out parsedValue))
            BusNamespace = parsedValue;
        else
            missing.Add(SERVICEBUS_NAMESPACE);

        if (ParseConfigValue(DB_CONNECTION_STRING, out parsedValue))
            DbConnectionString = parsedValue;
        else
            missing.Add(DB_CONNECTION_STRING);

        if (ParseConfigValue(SERVICEBUS_CONNECTION_STRING, out parsedValue))
            BusConnectionString = parsedValue;
        else
            missing.Add(SERVICEBUS_CONNECTION_STRING);

        if (ParseConfigValue(STORAGE_CONNECTION_STRING, out parsedValue))
            BlobConnectionString = parsedValue;
        else
            missing.Add(STORAGE_CONNECTION_STRING);

        if (ParseConfigValue(APPLICATIONINSIGHTS_CONNECTION_STRING, out parsedValue))
            AppInsightsConnectionString = parsedValue;
    }

    private bool ParseConfigValue(string key, out string? parsedValue)
    {
        parsedValue = configuration!.GetValue<string>(key);
        return !string.IsNullOrWhiteSpace(parsedValue);
    }

    public string? AppInsightsConnectionString { get; set; }
    public string? BusNamespace { get; set; }
    public string? DbConnectionString { get; set; }
    public string? Environment { get; set; }
    public string? Feature { get; set; }
    public string? BlobConnectionString { get; set; }
    public string? BusConnectionString { get; set; }
}
