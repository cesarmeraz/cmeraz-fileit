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
}
