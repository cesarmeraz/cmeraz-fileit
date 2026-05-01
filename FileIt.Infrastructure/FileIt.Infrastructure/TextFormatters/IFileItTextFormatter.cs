using Serilog.Formatting;

namespace FileIt.Infrastructure.TextFormatters;

public interface IFileItTextFormatter : ITextFormatter
{
    string GetHeader();
    string GetFileExtension();
}
