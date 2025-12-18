namespace FileIt.SqlProvider.Features.Simple
{
    public class SimpleConfig
    {
        public int SimpleIntakeEventId { get; set; }
        public int SimpleSubscriberEventId { get; set; }
        public int SimpleTestEventId { get; set; }

        public required string ApiAddQueueName { get; set; }
        public required string ApiAddTopicName { get; set; }
        public required string FeatureName { get; set; }
        public required string QueueName { get; set; }
        public required string SourceContainer { get; set; }
        public required string WorkingContainer { get; set; }
        public required string FinalContainer { get; set; }
    }
}
