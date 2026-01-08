using FileIt.Common.Domain;
using FileIt.Common.Tools;

namespace FileIt.App
{
    public class AppConfig : FeatureConfig
    {
        public int AddEventId { get; set; }
        public string? ApiAddTopicName { get; set; }
    }
}
