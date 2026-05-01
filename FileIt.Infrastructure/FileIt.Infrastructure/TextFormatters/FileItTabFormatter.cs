using System.Text;
using Serilog.Events;
using Serilog.Formatting;

namespace FileIt.Infrastructure.TextFormatters;

public class FileItTabFormatter : BaseTextFormatter, ITextFormatter, IFileItTextFormatter
{
    public override string GetHeader()
    {
        return "Timestamp\tLevel\tEventId\tMachineName\tApplication\tApplicationVersion\tInfrastructureVersion\tInvocationId\tCorrelationId\tMessage\tException";
    }

    public override string GetFileExtension()
    {
        return "tsv";
    }

    public override void Format(LogEvent logEvent, TextWriter output)
    {
        var builder = new StringBuilder();

        builder.Append(logEvent.Timestamp.ToString("o"));
        builder.Append('\t');
        builder.Append(logEvent.Level);
        builder.Append('\t');
        builder.Append(GetEventIdName(logEvent));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "MachineName", string.Empty));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "Application", string.Empty));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "ApplicationVersion", string.Empty));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "InfrastructureVersion", string.Empty));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "InvocationId", string.Empty));
        builder.Append('\t');
        builder.Append(GetValue(logEvent, "CorrelationId", string.Empty));
        builder.Append('\t');
        builder.Append(logEvent.RenderMessage());
        builder.Append('\t');
        builder.Append(logEvent.Exception);
        builder.AppendLine();
        output.Write(builder.ToString());
    }
}
