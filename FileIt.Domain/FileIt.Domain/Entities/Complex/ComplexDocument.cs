namespace FileIt.Domain.Entities.Complex;

/// <summary>
/// Mirrors dbo.ComplexDocument. The internal long Id is for storage joins;
/// PublicId (Guid) is what the API exposes externally.
/// </summary>
public class ComplexDocument
{
    public long DocumentId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public long SizeBytes { get; set; }
    public string? Content { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedUtc { get; set; }
    public string CreatedBy { get; set; } = "system";

    public byte[]? RowVersion { get; set; }

    public bool IsDeleted => DeletedUtc.HasValue;
}
