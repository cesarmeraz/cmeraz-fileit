namespace FileIt.App.Features.Simple
{
    public class SimpleConfig
    {
        public int SimpleIntakeEventId { get; set; }
        public int SimpleSubscriberEventId { get; set; }
        public int SimpleTestEventId { get; set; }

        public string ApiAddQueueName { get; set; }
        public string ApiAddTopicName { get; set; }
        public string FeatureName { get; set; }
        public string QueueName { get; set; }
        public string SourceContainer { get; set; }
        public string WorkingContainer { get; set; }
        public string FinalContainer { get; set; }
    }
}
