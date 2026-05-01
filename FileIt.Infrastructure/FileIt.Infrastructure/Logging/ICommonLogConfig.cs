namespace FileIt.Infrastructure.Logging;

public interface ICommonLogConfig
{
    string? Agent { get; set; }
    string? Environment { get; set; }
    string? Application { get; set; }
    string? ApplicationVersion { get; set; }
    string? CommonVersion { get; set; }
    string? Host { get; set; }
    string? LogFilePath { get; set; }
    string? SerilogSelfLogFilePath { get; set; }
    string? DbConnectionString { get; set; }
    string? AppInsightsConnectionString { get; set; }
    string? LogFormatterKey { get; set; }
    string? LogExpression { get; set; }
}

public class CommonLogConfig : ICommonLogConfig
{
    public string? Agent { get; set; }
    public string? DbConnectionString { get; set; }
    public string? Environment { get; set; }
    public string? Application { get; set; }
    public string? ApplicationVersion { get; set; }
    public string? CommonVersion { get; set; }
    public string? Host { get; set; }
    public string? LogFilePath { get; set; }
    public string? SerilogSelfLogFilePath { get; set; }
    public string? AppInsightsConnectionString { get; set; }
    public string? LogFormatterKey { get; set; }
    public string? LogExpression { get; set; }
}
