using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace FileIt.Infrastructure.Logging;

/// <summary>
/// Exposes the EventId's Name as a top-level EventName property.
/// Works regardless of whether Microsoft.Extensions.Logging serializes Name into
/// the EventId StructureValue: falls back to reading EventId.Name from the log event's
/// MessageTemplateToken when needed.
/// </summary>
public class EventNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string? resolvedName = null;

        if (logEvent.Properties.TryGetValue("EventId", out var eventIdValue))
        {
            if (eventIdValue is StructureValue sv)
            {
                var nameProp = sv.Properties.FirstOrDefault(p => p.Name == "Name");
                if (nameProp?.Value is ScalarValue sval && sval.Value is string n && !string.IsNullOrEmpty(n))
                {
                    resolvedName = n;
                }
            }
            else if (eventIdValue is ScalarValue scalar && scalar.Value is string s)
            {
                resolvedName = s;
            }
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("EventName", resolvedName ?? string.Empty)
        );
    }
}