using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;

namespace FileIt.Common.App;

public class CommonConfig
{
    public int AddEventId { get; set; }
    public string? ApiAddTopicName { get; set; }
    public string? ApiAddQueueName { get; set; }
}
