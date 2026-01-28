namespace FileIt.Domain.Entities.Api;

public class ApiAddResponse
{
    public string? Exception { get; set; }
    public string? StatusCode { get; set; }
    public int NodeId { get; set; }
    public string? CorrelationId { get; set; }
    public string TopicName { get; set; }
    public string Subject { get; set; }
}
