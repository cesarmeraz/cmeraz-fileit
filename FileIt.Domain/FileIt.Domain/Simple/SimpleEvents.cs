using Microsoft.Extensions.Logging;

namespace FileIt.Domain.Simple;

public class SimpleEvents
{
    public static EventId First = new EventId(1000, "FirstEvent");
}
