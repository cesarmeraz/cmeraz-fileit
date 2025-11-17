namespace FileIt.App.Models
{
    public class SimpleRequestLog
    {
        public int Id { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string? Agent { get; set; }
        public string? BlobName { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
