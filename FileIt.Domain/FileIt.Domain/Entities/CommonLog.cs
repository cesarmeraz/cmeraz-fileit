using FileIt.Domain.Interfaces;

namespace FileIt.Domain.Entities;

public class CommonLog : IAuditable
{
    public int Id { get; set; }

    public string? Message { get; set; }

    public string? MessageTemplate { get; set; }

    public string? Level { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }

    public string? Environment { get; set; }

    public string? MachineName { get; set; }

    public string? Feature { get; set; }

    public string? FeatureVersion { get; set; }
    public string? CommonVersion { get; set; }

    public string? SourceContext { get; set; }

    public string? CorrelationId { get; set; }

    public int? EventId { get; set; }

    public DateTime? CreatedOn { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
