namespace FileIt.App.Models
{
    public class AppConfig
    {
        public string ServiceBusNamespace { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
    }
}
