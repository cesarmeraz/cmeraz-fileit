using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface IFeatureConfig
{
    string? Agent { get; set; }
    string? AppInsightsConnectionString { get; set; }
    string? BusNamespace { get; set; }
    string? DbConnectionString { get; set; }
    string? Environment { get; set; }
    string? Feature { get; set; }
    string? FeatureVersion { get; set; }
    string? CommonVersion { get; set; }
    string? Host { get; set; }
    string? LogFilePath { get; set; }
    string? SerilogSelfLogFilePath { get; set; }
    string? BlobConnectionString { get; set; }
    string? BusConnectionString { get; set; }
    IEnumerable<string> QueueOrTopicNames { get; set; }
}
