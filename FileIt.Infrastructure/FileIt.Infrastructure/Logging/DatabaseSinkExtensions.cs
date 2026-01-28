using FileIt.Domain.Interfaces;
using Serilog;
using Serilog.Configuration;

namespace FileIt.Infrastructure.Logging;

public static class DatabaseSinkExtensions
{
    public static LoggerConfiguration DatabaseSink(
        this LoggerSinkConfiguration loggerConfiguration,
        IFeatureConfig featureConfig,
        IFormatProvider? formatProvider = null
    )
    {
        return loggerConfiguration.Sink(new DatabaseSink(formatProvider, featureConfig));
    }
}
