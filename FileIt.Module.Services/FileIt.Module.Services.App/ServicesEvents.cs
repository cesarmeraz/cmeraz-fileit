using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.App;

public class ServicesEvents
{
    public static EventId AddEvent = new EventId(1000, nameof(AddEvent));

    public static EventId GetPayload = new EventId(1001, nameof(GetPayload));
    public static EventId ExecApiAdd = new EventId(1002, nameof(ExecApiAdd));
    public static EventId LogApiAddRequest = new EventId(1003, nameof(LogApiAddRequest));
    public static EventId ApiAddResponsePublished = new EventId(
        1004,
        nameof(ApiAddResponsePublished)
    );
}
