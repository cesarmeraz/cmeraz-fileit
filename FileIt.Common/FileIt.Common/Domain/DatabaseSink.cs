using System.Data.Common;
using System.Text.Json;
using FileIt.Common.Data;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace FileIt.Common.Domain;

public class DatabaseSink : ILogEventSink
{
    private readonly IFormatProvider? _formatProvider;
    private readonly IFeatureConfig _featureConfig;
    private readonly string? _connectionString;

    public DatabaseSink(IFeatureConfig featureConfig)
    {
        _formatProvider = null;
        _featureConfig = featureConfig;
        _connectionString = featureConfig.DbConnectionString;
    }

    public DatabaseSink(IFormatProvider? formatProvider, IFeatureConfig featureConfig)
    {
        _formatProvider = formatProvider;
        _featureConfig = featureConfig;
        _connectionString = featureConfig.DbConnectionString;
    }

    private string? GetString(
        IReadOnlyDictionary<string, LogEventPropertyValue> properties,
        string key
    )
    {
        string? result = null;
        if (
            properties.TryGetValue(key, out LogEventPropertyValue? value)
            && value is ScalarValue sv
            && sv.Value is string rawValue
        )
        {
            result = rawValue;
        }
        return result;
    }

    private int? GetInt(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string key)
    {
        int? result = null;
        if (
            properties.TryGetValue(key, out LogEventPropertyValue? value)
            && value is ScalarValue sv
            && sv.Value is int rawValue
        )
        {
            result = rawValue;
        }
        return result;
    }

    public void Emit(LogEvent logEvent)
    {
        if (string.IsNullOrWhiteSpace(this._connectionString))
        {
            // without a connection string, logging will not work
            // TODO: if this is a critical problem, throw exception
            // instead of failing silently
            return;
        }
        var properties = logEvent.Properties;
        CommonLog entry = new CommonLog()
        {
            //true, since deployment
            CommonVersion = System
                .Reflection.Assembly.GetExecutingAssembly()
                .GetName()
                .Version?.ToString(),
            Environment = _featureConfig.Environment,
            Feature = _featureConfig.Feature,
            FeatureVersion = _featureConfig.FeatureVersion,
            MachineName = _featureConfig.Host,

            //the event only
            CreatedOn = logEvent.Timestamp.DateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(_formatProvider),
            ModifiedOn = logEvent.Timestamp.DateTime,
            MessageTemplate = logEvent.MessageTemplate.Text,

            //only available in properties
            CorrelationId = GetString(properties, "CorrelationId"),
            EventId = GetInt(properties, "EventId"),
            Exception = GetString(properties, "Exception"),
            SourceContext = GetString(properties, "SourceContext"),
            Properties = JsonSerializer.Serialize(logEvent.Properties),
        };

        using (var db = new CommonDbContext(_connectionString))
        {
            db.CommonLogs.Add(entry);
            db.SaveChanges();
        }
    }
}
