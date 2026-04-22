using System.Text;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Formatting;

namespace FileIt.Infrastructure.Logging;

public class MarkdownFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        if (logEvent == null)
            throw new ArgumentNullException(nameof(logEvent));
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        int? eventId = null;
        string? eventName = null;

        if (
            logEvent.Properties.TryGetValue("EventId", out var eventIdValue)
            && eventIdValue is StructureValue eventIdStruct
        )
        {
            foreach (var p in eventIdStruct.Properties)
            {
                if (p.Name == "Id" && p.Value is ScalarValue { Value: var v1 } && v1 is int i)
                    eventId = i;

                if (p.Name == "Name" && p.Value is ScalarValue { Value: var v2 } && v2 is string s)
                    eventName = s;
            }
        }

        string? applicationName = ParseProperty(logEvent, "Application", "Unknown");
        string? machineName = ParseProperty(logEvent, "MachineName", "Unknown");
        string? version = ParseProperty(logEvent, "ApplicationVersion", "Unknown");
        string? infrastructureVersion = ParseProperty(logEvent, "InfrastructureVersion", "Unknown");
        string? correlationId = ParseProperty(logEvent, "CorrelationId", "Unknown");
        string? invocationId = ParseProperty(logEvent, "InvocationId", "Unknown");
        string? exception = logEvent.Exception?.ToString();
        string message = logEvent.RenderMessage();

        bool isServices =
            applicationName != null
            && applicationName.Contains("Services", StringComparison.OrdinalIgnoreCase);
        string prefix = isServices ? "> " : "";

        var time = logEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
        var template = new StringBuilder();
        if (eventId == 1)
        {
            // Special case for the header
            template.AppendLine($"# Local Log File for {applicationName}");
            template.AppendLine($"- Generated on: {logEvent.Timestamp.ToLocalTime():o}");
            template.AppendLine($"- Machine: {machineName}");
            template.AppendLine($"- Application Version: {version}");
            template.AppendLine($"- Infrastructure Version: {infrastructureVersion}");
        }
        else
        {
            template.Append(prefix);
            template.AppendLine($"### {applicationName} - {version}");
            template.Append(prefix);
            template.AppendLine($"- Invocation Id: {invocationId}");
            template.Append(prefix);
            template.AppendLine($"- Correlation Id: {correlationId}");
            template.Append(prefix);
            template.AppendLine($"- {time} [{logEvent.Level}] {eventId}");
            template.Append(prefix);
            template.AppendLine($"- Message: {message}");
            if (!string.IsNullOrEmpty(exception))
            {
                template.Append(prefix);
                template.AppendLine($"- **Exception:**");
                template.Append(prefix);
                template.AppendLine("```");
                template.Append(prefix);
                template.AppendLine(exception);
                template.Append(prefix);
                template.AppendLine("```");
            }
        }
        output.WriteLine(template.ToString());
    }

    private string? ParseProperty(
        LogEvent logEvent,
        string propertyName,
        string? defaultValue = null
    )
    {
        if (
            logEvent.Properties.TryGetValue(propertyName, out LogEventPropertyValue? value)
            && value is ScalarValue { Value: string app }
        )
        {
            return app;
        }

        return defaultValue;
    }
}
