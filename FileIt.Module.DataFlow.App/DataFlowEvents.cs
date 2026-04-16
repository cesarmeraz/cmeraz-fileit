// These are our named event IDs for logging throughout the DataFlow module.
// Every meaningful step in the flow gets its own event ID so we can trace
// exactly where something succeeded or failed in the logs.
// We start at 3000 to keep our events separate from SimpleFlow's (which starts at 2000).
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.App;

public class DataFlowEvents
{
    // Fired when the blob trigger picks up a new GL Account CSV file
    public static EventId DataFlowWatcher = new EventId(3000, nameof(DataFlowWatcher));

    // Fired when we write the initial request log record to the database
    public static EventId DataFlowWatcherAddRequestLog = new EventId(3001, nameof(DataFlowWatcherAddRequestLog));

    // Fired when we move the file from source to working container
    public static EventId DataFlowWatcherMoveToWorking = new EventId(3002, nameof(DataFlowWatcherMoveToWorking));

    // Fired when we put a message on the service bus queue to kick off the transform
    public static EventId DataFlowWatcherQueueTransform = new EventId(3003, nameof(DataFlowWatcherQueueTransform));

    // Fired when the subscriber picks up the transform response from the topic
    public static EventId DataFlowSubscriber = new EventId(3010, nameof(DataFlowSubscriber));

    // Fired when we successfully receive the transform completion message
    public static EventId DataFlowSubscriberReceive = new EventId(3011, nameof(DataFlowSubscriberReceive));

    // Fired when receiving the message fails for any reason
    public static EventId DataFlowSubscriberReceiveFailed = new EventId(3012, nameof(DataFlowSubscriberReceiveFailed));

    // Fired when we look up the original request log by correlation ID
    public static EventId DataFlowSubscriberGetRequestLog = new EventId(3013, nameof(DataFlowSubscriberGetRequestLog));

    // Fired when we can't find the request log — something went wrong upstream
    public static EventId DataFlowSubscriberRequestLogNotFound = new EventId(3014, nameof(DataFlowSubscriberRequestLogNotFound));

    // Fired when the blob name is missing from the request log record
    public static EventId DataFlowSubscriberBlobNameMissing = new EventId(3015, nameof(DataFlowSubscriberBlobNameMissing));

    // Fired when we move the exported file from working to final container
    public static EventId DataFlowSubscriberMoveToFinal = new EventId(3016, nameof(DataFlowSubscriberMoveToFinal));

    // Fired when we update the request log with the transform results
    public static EventId DataFlowSubscriberUpdateRequestLog = new EventId(3017, nameof(DataFlowSubscriberUpdateRequestLog));

    // Fired when the whole flow completes successfully end to end
    public static EventId DataFlowSubscriberCompleted = new EventId(3018, nameof(DataFlowSubscriberCompleted));

    // Fired when the transform logic itself runs — parsing, grouping, aggregating
    public static EventId DataFlowTransform = new EventId(3020, nameof(DataFlowTransform));

    // Fired when the transform produces its output summary
    public static EventId DataFlowTransformCompleted = new EventId(3021, nameof(DataFlowTransformCompleted));
}
