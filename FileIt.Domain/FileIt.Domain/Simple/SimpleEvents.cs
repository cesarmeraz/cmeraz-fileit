using Microsoft.Extensions.Logging;

namespace FileIt.Domain.Simple;

public class SimpleEvents
{
    public static EventId SimpleWatcher = new EventId(1000, nameof(SimpleWatcher));
    public static EventId SimpleWatcherCommand = new EventId(1001, nameof(SimpleWatcherCommand));

    public static EventId SimpleSubscriber = new EventId(1002, nameof(SimpleSubscriber));
    public static EventId SimpleSubscriberCommand = new EventId(
        1002,
        nameof(SimpleSubscriberCommand)
    );
    public static EventId SimpleTest = new EventId(1003, nameof(SimpleTest));
}
