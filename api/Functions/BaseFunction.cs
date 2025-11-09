using Microsoft.Extensions.Logging;

namespace FileIt.Api.Functions;

public partial class BaseFunction
{
    public ILogger logger;

    public BaseFunction(ILogger logger)
    {
        this.logger = logger;
    }

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Function Start: {FunctionName}"
    )]
    public partial void LogFunctionStart(string functionName);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Function End: {FunctionName}"
    )]
    public partial void LogFunctionEnd(string functionName);
}
