using Microsoft.Extensions.Logging;

namespace FileIt.Module.SimpleFlow.App;

public class SimpleEvents
{
    public static EventId SimpleWatcher = new EventId(2000, nameof(SimpleWatcher));
    public static EventId SimpleWatcherAddRequestLog = new EventId(
        2001,
        nameof(SimpleWatcherAddRequestLog)
    );
    public static EventId SimpleWatcherMoveToWorking = new EventId(
        2002,
        nameof(SimpleWatcherMoveToWorking)
    );
    public static EventId SimpleWatcherQueueApiAdd = new EventId(
        2003,
        nameof(SimpleWatcherQueueApiAdd)
    );

    public static EventId SimpleSubscriber = new EventId(2010, nameof(SimpleSubscriber));
    public static EventId SimpleSubscriberReceive = new EventId(
        2011,
        nameof(SimpleSubscriberReceive)
    );
    public static EventId SimpleSubscriberReceiveFailed = new EventId(
        2012,
        nameof(SimpleSubscriberReceiveFailed)
    );

    public static EventId SimpleSubscriberMessage = new EventId(
        2013,
        nameof(SimpleSubscriberMessage)
    );
    public static EventId SimpleSubscriberGetRequestLog = new EventId(
        2014,
        nameof(SimpleSubscriberGetRequestLog)
    );
    public static EventId SimpleSubscriberRequestLogNotFound = new EventId(
        2015,
        nameof(SimpleSubscriberRequestLogNotFound)
    );
    public static EventId SimpleSubscriberBlobNameMissing = new EventId(
        2016,
        nameof(SimpleSubscriberBlobNameMissing)
    );
    public static EventId SimpleSubscriberMoveToFinal = new EventId(
        2017,
        nameof(SimpleSubscriberMoveToFinal)
    );
    public static EventId SimpleSubscriberUpdateRequestLog = new EventId(
        2018,
        nameof(SimpleSubscriberUpdateRequestLog)
    );
    public static EventId SimpleSubscriberCompleted = new EventId(
        2019,
        nameof(SimpleSubscriberCompleted)
    );

    public static EventId SimpleTest = new EventId(2030, nameof(SimpleTest));
}
