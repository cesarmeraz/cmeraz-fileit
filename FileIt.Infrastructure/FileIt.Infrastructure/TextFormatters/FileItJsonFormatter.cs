using System.Text;
using Serilog.Events;
using Serilog.Formatting;

namespace FileIt.Infrastructure.TextFormatters;

public class FileItJsonFormatter : BaseTextFormatter, ITextFormatter, IFileItTextFormatter
{
    public override string GetHeader()
    {
        return string.Empty;
    }

    public override string GetFileExtension()
    {
        return "json";
    }

    public override void Format(LogEvent logEvent, TextWriter output)
    {
        var builder = new StringBuilder();

        var eventId = GetEventIdName(logEvent);

        builder.AppendLine("{");
        AppendJsonProperty(builder, "Timestamp", logEvent.Timestamp.ToString("o"), true);
        AppendJsonProperty(builder, "Level", logEvent.Level.ToString(), true);
        AppendJsonProperty(builder, "Message", logEvent.RenderMessage(), true);
        AppendJsonProperty(
            builder,
            "MachineName",
            GetValue(logEvent, "MachineName", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "Application",
            GetValue(logEvent, "Application", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "ApplicationVersion",
            GetValue(logEvent, "ApplicationVersion", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "InfrastructureVersion",
            GetValue(logEvent, "InfrastructureVersion", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "SourceContext",
            GetValue(logEvent, "SourceContext", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "CorrelationId",
            GetValue(logEvent, "CorrelationId", string.Empty),
            true
        );
        AppendJsonProperty(
            builder,
            "InvocationId",
            GetValue(logEvent, "InvocationId", string.Empty),
            true
        );

        builder.Append("\t\"EventId\": ");
        builder.Append(eventId != null ? $"{eventId}" : "null");
        builder.AppendLine(",");

        AppendJsonProperty(builder, "Exception", logEvent.Exception?.ToString(), false);
        builder.AppendLine();
        builder.AppendLine("},");

        output.Write(builder.ToString());
    }

    private static void AppendJsonProperty(
        StringBuilder builder,
        string propertyName,
        string? value,
        bool appendTrailingComma
    )
    {
        builder.Append('\t');
        builder.Append('"');
        builder.Append(propertyName);
        builder.Append("\": ");
        builder.Append('"');
        builder.Append(EscapeJsonString(value));
        builder.Append('"');

        if (appendTrailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static string EscapeJsonString(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
