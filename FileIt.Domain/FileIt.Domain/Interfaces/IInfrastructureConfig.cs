using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface IInfrastructureConfig
{
    string? AppInsightsConnectionString { get; set; }
    string? BusNamespace { get; set; }
    string? DbConnectionString { get; set; }
    string? Environment { get; set; }
    string? Feature { get; set; }
    string? BlobConnectionString { get; set; }
    string? BusConnectionString { get; set; }
}
