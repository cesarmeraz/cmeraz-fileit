namespace FileIt.Common.Domain
{
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
    }

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
    }
}
