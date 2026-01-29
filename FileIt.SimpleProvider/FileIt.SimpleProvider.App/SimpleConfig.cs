using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;

namespace FileIt.SimpleProvider.App;

public class SimpleConfig
{
    public int SimpleIntakeEventId { get; set; }
    public int SimpleSubscriberEventId { get; set; }

    public int SimpleTestEventId { get; set; }

    public string ApiAddQueueName { get; set; } = string.Empty;
    public string ApiAddTopicName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string SourceContainer { get; set; } = string.Empty;
    public string WorkingContainer { get; set; } = string.Empty;
    public string FinalContainer { get; set; } = string.Empty;
}
