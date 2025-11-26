namespace FileIt.App.Models
{
    public class Api
    {
        public int Id { get; set; }
        public required string RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}