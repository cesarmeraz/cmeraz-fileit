using FileIt.Infrastructure.TextFormatters;
using Serilog.Events;
using Serilog.Formatting;

public abstract class BaseTextFormatter : IFileItTextFormatter
{
    public abstract void Format(LogEvent logEvent, TextWriter output);
    public abstract string GetHeader();
    public abstract string GetFileExtension();

    protected string GetEventIdName(LogEvent logEvent, string defaultValue = "")
    {
        if (
            logEvent.Properties.TryGetValue("EventIdName", out var eventIdValue)
            && eventIdValue is ScalarValue scalarValue
            && scalarValue.Value is string eventIdName
        )
        {
            return eventIdName;
        }
        return defaultValue;
    }

    protected int? GetEventIdId(LogEvent logEvent)
    {
        if (
            logEvent.Properties.TryGetValue("EventIdId", out var eventIdValue)
            && eventIdValue is ScalarValue scalarValue
            && scalarValue.Value is int eventIdId
        )
        {
            return eventIdId;
        }
        return default;
    }

    protected string? GetValue(LogEvent logEvent, string propertyName, string? defaultValue = null)
    {
        if (
            logEvent.Properties.TryGetValue(propertyName, out var propertyValue)
            && propertyValue is ScalarValue scalarValue
            && scalarValue.Value is string value
        )
        {
            return value;
        }
        return default;
    }
}
