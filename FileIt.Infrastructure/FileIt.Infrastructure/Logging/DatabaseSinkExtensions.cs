using Serilog;
using Serilog.Configuration;

namespace FileIt.Infrastructure.Logging;

public static class DatabaseSinkExtensions
{
    public static LoggerConfiguration DatabaseSink(
        this LoggerSinkConfiguration loggerConfiguration,
        ICommonLogConfig commonLogConfig,
        IFormatProvider? formatProvider = null
    )
    {
        return loggerConfiguration.Sink(new DatabaseSink(commonLogConfig, formatProvider));
    }
}
