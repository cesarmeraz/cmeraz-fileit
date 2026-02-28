using Microsoft.Extensions.Logging;

namespace FileIt.Common.App;

public class CommonEvents
{
    public static EventId AddEvent = new EventId(1000, nameof(AddEvent));
    public static EventId AddEventCommand = new EventId(1001, nameof(AddEventCommand));
}
