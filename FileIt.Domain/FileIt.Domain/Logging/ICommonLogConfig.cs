namespace FileIt.Domain.Logging;

public interface ICommonLogConfig
{
    string? Agent { get; set; }
    string? Environment { get; set; }
    string? Feature { get; set; }
    string? FeatureVersion { get; set; }
    string? CommonVersion { get; set; }
    string? Host { get; set; }
    string? LogFilePath { get; set; }
    string? SerilogSelfLogFilePath { get; set; }
    string? DbConnectionString { get; set; }
    string? AppInsightsConnectionString { get; set; }
}

public class CommonLogConfig : ICommonLogConfig
{
    public string? Agent { get; set; }
    public string? DbConnectionString { get; set; }
    public string? Environment { get; set; }
    public string? Feature { get; set; }
    public string? FeatureVersion { get; set; }
    public string? CommonVersion { get; set; }
    public string? Host { get; set; }
    public string? LogFilePath { get; set; }
    public string? SerilogSelfLogFilePath { get; set; }
    public string? AppInsightsConnectionString { get; set; }
}
