using Serilog.Core;
using Serilog.Events;

namespace FileIt.Infrastructure.Logging;

public sealed class EventIdDimensionsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out var eventIdProperty))
        {
            return;
        }

        if (eventIdProperty is StructureValue eventIdStructure)
        {
            foreach (var prop in eventIdStructure.Properties)
            {
                if (prop.Name == "Id" && prop.Value is ScalarValue idValue && idValue.Value != null)
                {
                    logEvent.AddPropertyIfAbsent(
                        propertyFactory.CreateProperty("EventIdId", idValue.Value)
                    );
                }

                if (
                    prop.Name == "Name"
                    && prop.Value is ScalarValue nameValue
                    && nameValue.Value is string eventName
                    && !string.IsNullOrWhiteSpace(eventName)
                )
                {
                    logEvent.AddPropertyIfAbsent(
                        propertyFactory.CreateProperty("EventIdName", eventName)
                    );
                }
            }

            return;
        }

        if (eventIdProperty is ScalarValue scalarValue && scalarValue.Value != null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("EventIdId", scalarValue.Value)
            );
        }
    }
}
