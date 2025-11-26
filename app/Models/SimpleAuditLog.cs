namespace FileIt.App.Models
{
    public class SimpleAuditLog
    {
        public int Id { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}