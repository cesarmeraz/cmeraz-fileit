using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure;

public class InfrastructureEvents
{
    public static readonly EventId First = new EventId(1, nameof(First));
    public static readonly EventId FunctionStart = new EventId(2, nameof(FunctionStart));
    public static readonly EventId FunctionEnd = new EventId(3, nameof(FunctionEnd));

    public static readonly EventId BlobToolMoveStart = new EventId(10, nameof(BlobToolMoveStart));
    public static readonly EventId BlobToolBlobNotFound = new EventId(11, nameof(BlobToolBlobNotFound));
    public static readonly EventId BlobToolMoved = new EventId(12, nameof(BlobToolMoved));
    public static readonly EventId BlobToolMoveFailed = new EventId(13, nameof(BlobToolMoveFailed));
    public static readonly EventId BlobToolUnexpected = new EventId(14, nameof(BlobToolUnexpected));

    public static readonly EventId BlobToolGetFile = new EventId(15, nameof(BlobToolGetFile));
    public static readonly EventId BlobToolGetFileNotFound = new EventId(
        16,
        nameof(BlobToolGetFileNotFound)
    );
    public static readonly EventId BlobToolGetFileDownloaded = new EventId(
        17,
        nameof(BlobToolGetFileDownloaded)
    );
    public static readonly EventId BlobToolGetFileRequestFailed = new EventId(
        18,
        nameof(BlobToolGetFileRequestFailed)
    );
    public static readonly EventId BlobToolGetFileUnexpected = new EventId(
        19,
        nameof(BlobToolGetFileUnexpected)
    );

    public static readonly EventId BlobToolUploadStart = new EventId(20, nameof(BlobToolUploadStart));
    public static readonly EventId BlobToolUploadError = new EventId(21, nameof(BlobToolUploadError));

    public static readonly EventId BusToolSendMessageValidationError = new EventId(
        30,
        nameof(BusToolSendMessageValidationError)
    );
    public static readonly EventId BusToolSendMessageStart = new EventId(
        31,
        nameof(BusToolSendMessageStart)
    );
    public static readonly EventId BusToolSendMessageEnqueued = new EventId(
        32,
        nameof(BusToolSendMessageEnqueued)
    );

    public static readonly EventId PublishToolEmitInvalid = new EventId(40, nameof(PublishToolEmitInvalid));
    public static readonly EventId PublishToolEmitStart = new EventId(41, nameof(PublishToolEmitStart));
    public static readonly EventId PublishToolEmitError = new EventId(42, nameof(PublishToolEmitError));
    public static readonly EventId PublishToolEmitEnd = new EventId(43, nameof(PublishToolEmitEnd));

    // Dead-letter reader lifecycle + message handling (#22).
    // See docs/dead-letter-strategy.md Section 9.
    public static readonly EventId DeadLetterReaderStarted = new EventId(
        50,
        nameof(DeadLetterReaderStarted)
    );
    public static readonly EventId DeadLetterReaderStopped = new EventId(
        51,
        nameof(DeadLetterReaderStopped)
    );
    public static readonly EventId DeadLetterMessageReceived = new EventId(
        52,
        nameof(DeadLetterMessageReceived)
    );
    public static readonly EventId DeadLetterRecordPersisted = new EventId(
        53,
        nameof(DeadLetterRecordPersisted)
    );
    public static readonly EventId DeadLetterRecordPersistFailed = new EventId(
        54,
        nameof(DeadLetterRecordPersistFailed)
    );
    public static readonly EventId DeadLetterClassified = new EventId(
        55,
        nameof(DeadLetterClassified)
    );
    public static readonly EventId DeadLetterClassificationUnknown = new EventId(
        56,
        nameof(DeadLetterClassificationUnknown)
    );

    // Replay function lifecycle + per-record outcomes (#22).
    public static readonly EventId ReplayFunctionStarted = new EventId(
        60,
        nameof(ReplayFunctionStarted)
    );
    public static readonly EventId ReplayFunctionStopped = new EventId(
        61,
        nameof(ReplayFunctionStopped)
    );
    public static readonly EventId ReplayInitiated = new EventId(62, nameof(ReplayInitiated));
    public static readonly EventId ReplaySucceeded = new EventId(63, nameof(ReplaySucceeded));
    public static readonly EventId ReplayFailed = new EventId(64, nameof(ReplayFailed));
    public static readonly EventId ReplayExhausted = new EventId(65, nameof(ReplayExhausted));
    /// <summary>
    /// Unhandled exception caught by ExceptionHandlingMiddleware. Closes the gap
    /// identified in the #41 schema review where middleware-caught exceptions
    /// landed in CommonLog with NULL EventId/EventName.
    /// </summary>
    public static readonly EventId UnhandledException = new EventId(70, nameof(UnhandledException));
}
