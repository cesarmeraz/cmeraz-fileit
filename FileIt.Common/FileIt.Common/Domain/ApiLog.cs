namespace FileIt.Common.Domain;

public class ApiLog
{
    public int Id { get; set; }
    public string? ClientRequestId { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
