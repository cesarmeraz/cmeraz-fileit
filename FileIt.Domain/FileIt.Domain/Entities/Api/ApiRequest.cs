namespace FileIt.Domain.Entities.Api;

public class ApiRequest
{
    public ApiRequest(string messageId)
    {
        MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
    }

    public string? QueueName { get; set; }
    public string MessageId { get; set; }
    public string? Subject { get; set; }
    public string? ReplyTo { get; set; }
    public string? CorrelationId { get; set; }
    public object? Body { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is ApiRequest req && QueueName == req.QueueName && MessageId == req.MessageId;
    }

    public override int GetHashCode()
    {
        return (QueueName?.GetHashCode() ?? 0) ^ MessageId.GetHashCode();
    }

    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
