namespace FileIt.Module.Ui.Host;

public class CommonLogDetailDto
{
    public int Id { get; set; }

    public string? Message { get; set; }

    public string? Level { get; set; }

    public string? Exception { get; set; }

    public string? Environment { get; set; }

    public string? MachineName { get; set; }

    public string? Application { get; set; }

    public string? ApplicationVersion { get; set; }

    public string? InfrastructureVersion { get; set; }

    public string? SourceContext { get; set; }

    public string? CorrelationId { get; set; }

    public string? InvocationId { get; set; }

    public string? EventName { get; set; }

    public DateTime? CreatedOn { get; set; }
}
