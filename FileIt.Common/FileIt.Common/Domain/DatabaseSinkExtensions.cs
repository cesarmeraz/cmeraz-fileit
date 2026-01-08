using FileIt.Common.Domain;
using Serilog;
using Serilog.Configuration;

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
