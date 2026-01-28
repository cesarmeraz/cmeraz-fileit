public class InfrastructureConfig
{
    public string? BusNamespace { get; set; }
    public string? BusConnString { get; set; }
    public string? BlobConnString { get; set; }
    public string? DbConnString { get; set; }
    public IEnumerable<string> QueueOrTopicNames { get; set; } = new List<string>();
}
