using Microsoft.Extensions.Logging;

namespace FileIt.Domain.Common;

public class CommonEvents
{
    public static EventId First = new EventId(1000, "FirstEvent");
}
