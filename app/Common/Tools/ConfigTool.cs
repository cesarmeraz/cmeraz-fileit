using FileIt.App.Features.Api;
using FileIt.App.Features.Simple;

namespace FileIt.App.Common.Tools
{
    public class ConfigTool
    {
        public required ApiConfig Api { get; set; }
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
