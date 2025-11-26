namespace FileIt.App.Models
{
    public class SimpleRequestLog
    {
        public int Id { get; set; }
        public required string ClientRequestId { get; set; }
        public int ApiId { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}