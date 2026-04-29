using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule.App;

/// <summary>
/// EventId catalog for this module. The numeric base (9000) is allocated
/// per-module so logs from different modules don't collide on EventId values.
/// SimpleFlow=2000, DataFlow=3000, Services=1000.
/// </summary>
public class FileItModuleEvents
{
    public static EventId FileItModuleWatcher = new EventId(9000, nameof(FileItModuleWatcher));
    public static EventId FileItModuleWatcherAddRequestLog = new EventId(
        9001,
        nameof(FileItModuleWatcherAddRequestLog)
    );
    public static EventId FileItModuleWatcherMoveToWorking = new EventId(
        9002,
        nameof(FileItModuleWatcherMoveToWorking)
    );
    public static EventId FileItModuleWatcherQueueApiAdd = new EventId(
        9003,
        nameof(FileItModuleWatcherQueueApiAdd)
    );

    public static EventId FileItModuleSubscriber = new EventId(9010, nameof(FileItModuleSubscriber));
    public static EventId FileItModuleSubscriberReceive = new EventId(
        9011,
        nameof(FileItModuleSubscriberReceive)
    );
    public static EventId FileItModuleSubscriberReceiveFailed = new EventId(
        9012,
        nameof(FileItModuleSubscriberReceiveFailed)
    );
    public static EventId FileItModuleSubscriberMessage = new EventId(
        9013,
        nameof(FileItModuleSubscriberMessage)
    );
    public static EventId FileItModuleSubscriberGetRequestLog = new EventId(
        9014,
        nameof(FileItModuleSubscriberGetRequestLog)
    );
    public static EventId FileItModuleSubscriberRequestLogNotFound = new EventId(
        9015,
        nameof(FileItModuleSubscriberRequestLogNotFound)
    );
    public static EventId FileItModuleSubscriberBlobNameMissing = new EventId(
        9016,
        nameof(FileItModuleSubscriberBlobNameMissing)
    );
    public static EventId FileItModuleSubscriberMoveToFinal = new EventId(
        9017,
        nameof(FileItModuleSubscriberMoveToFinal)
    );
    public static EventId FileItModuleSubscriberUpdateRequestLog = new EventId(
        9018,
        nameof(FileItModuleSubscriberUpdateRequestLog)
    );
    public static EventId FileItModuleSubscriberCompleted = new EventId(
        9019,
        nameof(FileItModuleSubscriberCompleted)
    );

    public static EventId FileItModuleTest = new EventId(9030, nameof(FileItModuleTest));
}
