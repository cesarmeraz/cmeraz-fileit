using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Logging;

public class InfrastructureEvents
{
    public static EventId First = new EventId(1, nameof(First));
    public static EventId FunctionStart = new EventId(2, nameof(FunctionEnd));
    public static EventId FunctionEnd = new EventId(3, nameof(FunctionEnd));

    public static EventId BlobToolMoveStart = new EventId(10, nameof(BlobToolMoveStart));
    public static EventId BlobToolBlobNotFound = new EventId(11, nameof(BlobToolBlobNotFound));
    public static EventId BlobToolMoved = new EventId(12, nameof(BlobToolMoved));
    public static EventId BlobToolMoveFailed = new EventId(13, nameof(BlobToolMoveFailed));
    public static EventId BlobToolUnexpected = new EventId(14, nameof(BlobToolUnexpected));

    public static EventId BlobToolGetFile = new EventId(15, nameof(BlobToolGetFile));
    public static EventId BlobToolUploadStart = new EventId(16, nameof(BlobToolUploadStart));
    public static EventId BlobToolUploadError = new EventId(17, nameof(BlobToolUploadError));

    public static EventId BusToolSendMessageValidationError = new EventId(
        20,
        nameof(BusToolSendMessageValidationError)
    );
    public static EventId BusToolSendMessageStart = new EventId(
        21,
        nameof(BusToolSendMessageStart)
    );
    public static EventId BusToolSendMessageEnqueued = new EventId(
        22,
        nameof(BusToolSendMessageEnqueued)
    );

    public static EventId PublishToolEmitInvalid = new EventId(30, nameof(PublishToolEmitInvalid));
    public static EventId PublishToolEmitStart = new EventId(31, nameof(PublishToolEmitStart));
    public static EventId PublishToolEmitError = new EventId(32, nameof(PublishToolEmitError));
    public static EventId PublishToolEmitEnd = new EventId(33, nameof(PublishToolEmitEnd));
}
