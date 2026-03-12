namespace FileIt.SimpleFlow.App;

public class SimpleConfig
{
    public string ApiAddQueueName { get; set; } = string.Empty;
    public string ApiAddTopicName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string SourceContainer { get; set; } = string.Empty;
    public string WorkingContainer { get; set; } = string.Empty;
    public string FinalContainer { get; set; } = string.Empty;
}
