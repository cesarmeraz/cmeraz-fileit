namespace FileIt.Module.Ui.Host;

public class CommonLogSummaryDto
{
    public int Id { get; set; }

    public string? Application { get; set; }

    public string? InvocationId { get; set; }

    public string? EventName { get; set; }

    public DateTime CreatedOn { get; set; }
}
