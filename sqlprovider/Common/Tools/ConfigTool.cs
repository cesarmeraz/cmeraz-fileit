using FileIt.SqlProvider.Features.Simple;

namespace FileIt.SqlProvider.Common.Tools
{
    public class ConfigTool
    {
        public required CommonConfig Common { get; set; }
        public required SimpleConfig Simple { get; set; }
    }

    public class CommonConfig
    {
        public required string BusNamespace { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
    }
}
