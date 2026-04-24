using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure;

public class InfrastructureEvents
{
    public static EventId First = new EventId(1, nameof(First));
    public static EventId FunctionStart = new EventId(2, nameof(FunctionStart));
    public static EventId FunctionEnd = new EventId(3, nameof(FunctionEnd));

    public static EventId BlobToolMoveStart = new EventId(10, nameof(BlobToolMoveStart));
    public static EventId BlobToolBlobNotFound = new EventId(11, nameof(BlobToolBlobNotFound));
    public static EventId BlobToolMoved = new EventId(12, nameof(BlobToolMoved));
    public static EventId BlobToolMoveFailed = new EventId(13, nameof(BlobToolMoveFailed));
    public static EventId BlobToolUnexpected = new EventId(14, nameof(BlobToolUnexpected));

    public static EventId BlobToolGetFile = new EventId(15, nameof(BlobToolGetFile));
    public static EventId BlobToolGetFileNotFound = new EventId(
        16,
        nameof(BlobToolGetFileNotFound)
    );
    public static EventId BlobToolGetFileDownloaded = new EventId(
        17,
        nameof(BlobToolGetFileDownloaded)
    );
    public static EventId BlobToolGetFileRequestFailed = new EventId(
        18,
        nameof(BlobToolGetFileRequestFailed)
    );
    public static EventId BlobToolGetFileUnexpected = new EventId(
        19,
        nameof(BlobToolGetFileUnexpected)
    );

    public static EventId BlobToolUploadStart = new EventId(20, nameof(BlobToolUploadStart));
    public static EventId BlobToolUploadError = new EventId(21, nameof(BlobToolUploadError));

    public static EventId BusToolSendMessageValidationError = new EventId(
        30,
        nameof(BusToolSendMessageValidationError)
    );
    public static EventId BusToolSendMessageStart = new EventId(
        31,
        nameof(BusToolSendMessageStart)
    );
    public static EventId BusToolSendMessageEnqueued = new EventId(
        32,
        nameof(BusToolSendMessageEnqueued)
    );

    public static EventId PublishToolEmitInvalid = new EventId(40, nameof(PublishToolEmitInvalid));
    public static EventId PublishToolEmitStart = new EventId(41, nameof(PublishToolEmitStart));
    public static EventId PublishToolEmitError = new EventId(42, nameof(PublishToolEmitError));
    public static EventId PublishToolEmitEnd = new EventId(43, nameof(PublishToolEmitEnd));

    // Dead-letter reader lifecycle + message handling (#22).
    // See docs/dead-letter-strategy.md Section 9.
    public static EventId DeadLetterReaderStarted = new EventId(
        50,
        nameof(DeadLetterReaderStarted)
    );
    public static EventId DeadLetterReaderStopped = new EventId(
        51,
        nameof(DeadLetterReaderStopped)
    );
    public static EventId DeadLetterMessageReceived = new EventId(
        52,
        nameof(DeadLetterMessageReceived)
    );
    public static EventId DeadLetterRecordPersisted = new EventId(
        53,
        nameof(DeadLetterRecordPersisted)
    );
    public static EventId DeadLetterRecordPersistFailed = new EventId(
        54,
        nameof(DeadLetterRecordPersistFailed)
    );
    public static EventId DeadLetterClassified = new EventId(
        55,
        nameof(DeadLetterClassified)
    );
    public static EventId DeadLetterClassificationUnknown = new EventId(
        56,
        nameof(DeadLetterClassificationUnknown)
    );

    // Replay function lifecycle + per-record outcomes (#22).
    public static EventId ReplayFunctionStarted = new EventId(
        60,
        nameof(ReplayFunctionStarted)
    );
    public static EventId ReplayFunctionStopped = new EventId(
        61,
        nameof(ReplayFunctionStopped)
    );
    public static EventId ReplayInitiated = new EventId(62, nameof(ReplayInitiated));
    public static EventId ReplaySucceeded = new EventId(63, nameof(ReplaySucceeded));
    public static EventId ReplayFailed = new EventId(64, nameof(ReplayFailed));
    public static EventId ReplayExhausted = new EventId(65, nameof(ReplayExhausted));
}
