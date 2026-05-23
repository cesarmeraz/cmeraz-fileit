// This is the database entity that tracks a GL Account CSV file
// as it moves through the DataFlow pipeline.
// Every file that comes in gets one of these records so we can trace
// exactly what happened to it from start to finish.
using System.Text.Json;
using FileIt.Domain.Interfaces;

namespace FileIt.Domain.Entities;

public class DataFlowRequestLog : IAuditable
{
    // Auto-generated database ID
    public int Id { get; set; }

    // Are we running locally, in dev, or in prod?
    public string Environment { get; set; } = string.Empty;

    // Which machine processed this file
    public string Host { get; set; } = string.Empty;

    // Who kicked this off — usually the service account
    public string? Agent { get; set; }

    // The name of the CSV file we picked up from blob storage
    public string? BlobName { get; set; }

    // The correlation ID we assigned when the file first arrived —
    // this ties together every log entry across the whole flow
    public string? ClientRequestId { get; set; }

    // How many rows were in the original CSV
    public int RowsIngested { get; set; }

    // How many groups came out of the transform
    public int RowsTransformed { get; set; }

    // The name of the output file we wrote to the final container
    public string? ExportBlobName { get; set; }

    // Where are we in the flow? New, Processing, Complete, Failed
    public string? Status { get; set; }

    // Any notes we want to attach — useful for error messages
    public string? Comment { get; set; }

    // When this record was first created
    public DateTime? CreatedOn { get; set; }

    // When this record was last updated
    public DateTime? ModifiedOn { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
