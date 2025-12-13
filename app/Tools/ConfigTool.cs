using FileIt.App.Features.Api;
using FileIt.App.Features.Simple;

namespace FileIt.App.Tools
{
    public class ConfigTool
    {
        public ApiConfig Api { get; set; }
        public CommonConfig Common { get; set; }
        public SimpleConfig Simple { get; set; }
    }

    public class CommonConfig
    {
        public string BusNamespace { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
    }
}
