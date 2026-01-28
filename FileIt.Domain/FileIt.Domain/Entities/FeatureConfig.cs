using FileIt.Domain.Interfaces;

namespace FileIt.Domain.Entities;

public abstract class FeatureConfig : IFeatureConfig
{
    public virtual string? Agent { get; set; }
    public virtual string? AppInsightsConnectionString { get; set; }
    public virtual string? BusNamespace { get; set; }
    public virtual string? DbConnectionString { get; set; }
    public virtual string? Environment { get; set; }
    public virtual string? Feature { get; set; }
    public virtual string? FeatureVersion { get; set; }
    public virtual string? CommonVersion { get; set; }
    public virtual string? Host { get; set; }
    public virtual string? LogFilePath { get; set; }
    public virtual string? SerilogSelfLogFilePath { get; set; }
    public virtual string? BlobConnectionString { get; set; }
    public virtual string? BusConnectionString { get; set; }
    public virtual IEnumerable<string> QueueOrTopicNames { get; set; } = new List<string>();
}
