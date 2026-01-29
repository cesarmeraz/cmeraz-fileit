using FileIt.Domain.Interfaces;

namespace FileIt.Infrastructure.DependencyInjection;

public class InfrastructureConfig : IInfrastructureConfig
{
    public string? AppInsightsConnectionString { get; set; }
    public string? BusNamespace { get; set; }
    public string? DbConnectionString { get; set; }
    public string? Environment { get; set; }
    public string? Feature { get; set; }
    public string? BlobConnectionString { get; set; }
    public string? BusConnectionString { get; set; }
}
