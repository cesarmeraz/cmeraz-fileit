using System.Text;
using Serilog.Events;
using Serilog.Formatting;

namespace FileIt.Infrastructure.TextFormatters;

public class FileItCsvFormatter : BaseTextFormatter, ITextFormatter, IFileItTextFormatter
{
    public override string GetHeader()
    {
        return "Timestamp,Level,EventId,MachineName,Application,ApplicationVersion,InfrastructureVersion,InvocationId,CorrelationId,Message,Exception";
    }

    public override string GetFileExtension()
    {
        return "csv";
    }

    public override void Format(LogEvent logEvent, TextWriter output)
    {
        var builder = new StringBuilder();

        var values = new string?[]
        {
            logEvent.Timestamp.ToString("o"),
            logEvent.Level.ToString(),
            GetEventIdName(logEvent),
            GetValue(logEvent, "MachineName", string.Empty),
            GetValue(logEvent, "Application", string.Empty),
            GetValue(logEvent, "ApplicationVersion", string.Empty),
            GetValue(logEvent, "InfrastructureVersion", string.Empty),
            GetValue(logEvent, "InvocationId", string.Empty),
            GetValue(logEvent, "CorrelationId", string.Empty),
            logEvent.RenderMessage(),
            logEvent.Exception?.ToString(),
        };

        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            AppendCsvValue(builder, values[i]);
        }

        builder.AppendLine();
        output.Write(builder.ToString());
    }

    private static void AppendCsvValue(StringBuilder builder, string? value)
    {
        builder.Append('"');
        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(value.Replace("\"", "\"\""));
        }
        builder.Append('"');
    }
}
